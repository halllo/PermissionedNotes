using com.authzed.api.v1;
using Grpc.Core;
using Microsoft.Extensions.Options;
using System.Collections.Immutable;

namespace Permissions
{
	public interface IPermissionsRepository
	{
		Task<bool> IsAllowed(string subjectType, Guid subjectId, string permission, string objectType, Guid objectId, string? writtenAtToken = default, CancellationToken cancellationToken = default);
		Task<IReadOnlySet<Guid>> IsAllowedBulkExperimental(IEnumerable<(string subjectType, Guid subjectId, string permission, string objectType, Guid objectId)> items, string? writtenAtToken = default, CancellationToken cancellationToken = default);
		IAsyncEnumerable<Guid> LookupResources(string subjectType, Guid subjectId, string permission, string objectType, string? writtenAtToken = default, CancellationToken cancellationToken = default);
		Task<IReadOnlyCollection<IRelation>> GetRelations(string objectType, Guid objectId, string relationType = "", string? writtenAtToken = default, CancellationToken cancellationToken = default);
		Task<string> UpdateRelations(string objectType, Guid objectId, Update dto, CancellationToken cancellationToken);
		Task Remove(string objectType, Guid objectId, CancellationToken cancellationToken);
	}

	public interface IRelation
	{
		string Relation { get; }
		string SubjectType { get; }
		Guid SubjectId { get; }
	}

	public class Update
	{
		public Relation[] RelationsToUpdate { get; set; } = null!;

		public class Relation
		{
			public string SubjectType { get; set; } = null!;
			public Guid SubjectId { get; set; }
			public string? SubjectRelation { get; set; }
			public string Name { get; set; } = null!;
			public Operation Operation { get; set; }
		}

		public enum Operation { Touch, Remove }
	}

	internal class PermissionsRepository : GrpcRepository, IPermissionsRepository
	{
		private readonly PermissionsService.PermissionsServiceClient permissionsServiceClient;
		private readonly ExperimentalService.ExperimentalServiceClient experimentalServiceClient;

		public PermissionsRepository(PermissionsService.PermissionsServiceClient permissionsServiceClient, ExperimentalService.ExperimentalServiceClient experimentalServiceClient, IOptionsMonitor<PermissionsOptions> options) : base(options)
		{
			this.permissionsServiceClient = permissionsServiceClient;
			this.experimentalServiceClient = experimentalServiceClient;
		}



		public async Task<bool> IsAllowed(string subjectType, Guid subjectId, string permission, string objectType, Guid objectId, string? writtenAtToken = default, CancellationToken cancellationToken = default)
		{
			var hasPermissionResponse = await permissionsServiceClient.CheckPermissionAsync(
				request: new CheckPermissionRequest
				{
					Consistency = writtenAtToken == null ? null : new Consistency
					{
						AtLeastAsFresh = new ZedToken
						{
							Token = writtenAtToken
						}
					},
					Subject = new SubjectReference
					{
						Object = new ObjectReference
						{
							ObjectType = subjectType,
							ObjectId = subjectId.ToString()
						}
					},
					Permission = permission,
					Resource = new ObjectReference
					{
						ObjectType = objectType,
						ObjectId = objectId.ToString(),
					},
				},
				headers: AuthHeaders(),
				cancellationToken: cancellationToken);

			return hasPermissionResponse.Permissionship == CheckPermissionResponse.Types.Permissionship.HasPermission;
		}


		public async Task<IReadOnlySet<Guid>> IsAllowedBulkExperimental(IEnumerable<(string subjectType, Guid subjectId, string permission, string objectType, Guid objectId)> items, string? writtenAtToken = default, CancellationToken cancellationToken = default)
		{
			var bulkPermissionResponse = await experimentalServiceClient.BulkCheckPermissionAsync(
				request: new BulkCheckPermissionRequest
				{
					Consistency = writtenAtToken switch
					{
						not null => new Consistency { AtLeastAsFresh = new ZedToken { Token = writtenAtToken! } },
						null => new Consistency { MinimizeLatency = true },
					},
					Items =
					{
						items.Select(i => new BulkCheckPermissionRequestItem
						{
							Subject = new SubjectReference
							{
								Object = new ObjectReference
								{
									ObjectType = i.subjectType,
									ObjectId = i.subjectId.ToString()
								}
							},
							Permission = i.permission,
							Resource = new ObjectReference
							{
								ObjectType = i.objectType,
								ObjectId = i.objectId.ToString(),
							},
						})
					}
				},
				headers: AuthHeaders(),
				cancellationToken: cancellationToken);

			return bulkPermissionResponse.Pairs
				.Where(p => p.Item.Permissionship == CheckPermissionResponse.Types.Permissionship.HasPermission)
				.Select(p => Guid.Parse(p.Request.Resource.ObjectId))
				.ToImmutableHashSet();
		}


		public IAsyncEnumerable<Guid> LookupResources(string subjectType, Guid subjectId, string permission, string objectType, string? writtenAtToken = default, CancellationToken cancellationToken = default)
		{
			var serverStream = permissionsServiceClient.LookupResources(
				request: new LookupResourcesRequest
				{
					Consistency = writtenAtToken == null ? null : new Consistency
					{
						AtLeastAsFresh = new ZedToken
						{
							Token = writtenAtToken
						}
					},
					ResourceObjectType = objectType,
					Permission = permission,
					Subject = new SubjectReference
					{
						Object = new ObjectReference
						{
							ObjectType = subjectType,
							ObjectId = subjectId.ToString()
						}
					},
					OptionalLimit = 0,
					//OptionalCursor = null,
				},
				headers: AuthHeaders(),
				cancellationToken: cancellationToken);

			var resources = serverStream.ResponseStream.ReadAllAsync(cancellationToken)
				.Where(r => r.Permissionship == LookupPermissionship.HasPermission)
				.Select(r => Guid.Parse(r.ResourceObjectId));

			return resources;
		}


		public async Task<IReadOnlyCollection<IRelation>> GetRelations(string objectType, Guid objectId, string relationType = "", string? writtenAtToken = default, CancellationToken cancellationToken = default)
		{
			var response = permissionsServiceClient.ReadRelationships(
				request: new ReadRelationshipsRequest
				{
					Consistency = writtenAtToken == null ? null : new Consistency
					{
						AtLeastAsFresh = new ZedToken
						{
							Token = writtenAtToken
						}
					},
					RelationshipFilter = new RelationshipFilter
					{
						ResourceType = objectType,
						OptionalResourceId = objectId.ToString(),
						OptionalRelation = relationType,
					}
				},
			headers: AuthHeaders(),
			cancellationToken: cancellationToken);

			var relationships = await response.ResponseStream.ReadAllAsync(cancellationToken)
				.Select(r => new RelationDto
				{
					ObjectId = Guid.Parse(r.Relationship.Resource.ObjectId),
					Relation = r.Relationship.Relation,
					SubjectId = Guid.Parse(r.Relationship.Subject.Object.ObjectId),
					SubjectType = r.Relationship.Subject.Object.ObjectType,
				})
				.ToListAsync(cancellationToken);

			return relationships;
		}
		class RelationDto : IRelation
		{
			public Guid ObjectId { get; set; }
			public string Relation { get; set; } = null!;
			public Guid SubjectId { get; set; }
			public string SubjectType { get; set; } = null!;
		}


		public async Task<string> UpdateRelations(string objectType, Guid objectId, Update dto, CancellationToken cancellationToken)
		{
			var updatables = dto.RelationsToUpdate.Select(p => new RelationshipUpdate
			{
				Operation = p.Operation switch
				{
					Update.Operation.Touch => RelationshipUpdate.Types.Operation.Touch,
					Update.Operation.Remove => RelationshipUpdate.Types.Operation.Delete,
					_ => RelationshipUpdate.Types.Operation.Unspecified
				},
				Relationship = new Relationship
				{
					Subject = new SubjectReference
					{
						Object = new ObjectReference
						{
							ObjectType = p.SubjectType,
							ObjectId = p.SubjectId.ToString(),
						},
						OptionalRelation = p.SubjectRelation ?? ""
					},
					Relation = p.Name,
					Resource = new ObjectReference
					{
						ObjectType = objectType,
						ObjectId = objectId.ToString(),
					}
				}
			}).ToList();

			var response = await permissionsServiceClient.WriteRelationshipsAsync(
				request: new WriteRelationshipsRequest
				{
					Updates = { updatables }
				},
				headers: AuthHeaders(),
				cancellationToken: cancellationToken);

			return response.WrittenAt.Token;
		}


		public async Task Remove(string objectType, Guid objectId, CancellationToken cancellationToken)
		{
			await permissionsServiceClient.DeleteRelationshipsAsync(
				new DeleteRelationshipsRequest
				{
					RelationshipFilter = new RelationshipFilter
					{
						ResourceType = objectType,
						OptionalResourceId = objectId.ToString()
					}
				},
				headers: AuthHeaders(),
				cancellationToken: cancellationToken);
		}
	}
}
