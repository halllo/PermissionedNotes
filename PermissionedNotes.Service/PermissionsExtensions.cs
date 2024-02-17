using Permissions;
using System.Security.Claims;

namespace PermissionedNotes.Service
{
	public static class PermissionChecks
	{
		public static Task RemoveTeam(this IPermissionsRepository permissions, Guid teamId, CancellationToken cancellationToken)
			=> permissions.Remove("team", teamId, cancellationToken);

		public static Task<IReadOnlyCollection<IRelation>> GetTeamRelations(this IPermissionsRepository permissions, Guid teamId, CancellationToken cancellationToken)
			=> permissions.GetRelations("team", teamId, cancellationToken: cancellationToken);

		public static Task<string> UpdateTeamRelations(this IPermissionsRepository permissions, Guid teamId, Update.Relation[] relations, CancellationToken cancellationToken)
			=> permissions.UpdateRelations("team", teamId, new Update { RelationsToUpdate = relations }, cancellationToken);



		public static IAsyncEnumerable<Guid> LookupCollections(this IPermissionsRepository permissions, ClaimsPrincipal user, CancellationToken cancellationToken)
			=> permissions.LookupResources("user", user.Id(), "read", "collection", cancellationToken: cancellationToken);

		public static Task<bool> IsAllowedToReadCollection(this IPermissionsRepository permissions, ClaimsPrincipal user, Guid collectionId, CancellationToken cancellationToken)
			=> permissions.IsAllowed("user", user.Id(), "read", "collection", collectionId, cancellationToken: cancellationToken);

		public static Task<bool> IsAllowedToUpdateCollection(this IPermissionsRepository permissions, ClaimsPrincipal user, Guid collectionId, CancellationToken cancellationToken)
			=> permissions.IsAllowed("user", user.Id(), "update", "collection", collectionId, cancellationToken: cancellationToken);

		public static Task<bool> IsAllowedToUpdateCollectionPermissions(this IPermissionsRepository permissions, ClaimsPrincipal user, Guid collectionId, CancellationToken cancellationToken)
			=> permissions.IsAllowed("user", user.Id(), "change_permissions", "collection", collectionId, cancellationToken: cancellationToken);

		public static Task<bool> IsAllowedToDeleteCollection(this IPermissionsRepository permissions, ClaimsPrincipal user, Guid collectionId, CancellationToken cancellationToken)
			=> permissions.IsAllowed("user", user.Id(), "delete", "collection", collectionId, cancellationToken: cancellationToken);

		public static Task<bool> IsAllowedToCreateNote(this IPermissionsRepository permissions, ClaimsPrincipal user, Guid collectionId, CancellationToken cancellationToken)
			=> permissions.IsAllowed("user", user.Id(), "create_note", "collection", collectionId, cancellationToken: cancellationToken);

		public static Task RemoveCollection(this IPermissionsRepository permissions, Guid collectionId, CancellationToken cancellationToken)
			=> permissions.Remove("collection", collectionId, cancellationToken);

		public static Task<IReadOnlyCollection<IRelation>> GetCollectionRelations(this IPermissionsRepository permissions, Guid collectionId, CancellationToken cancellationToken)
			=> permissions.GetRelations("collection", collectionId, cancellationToken: cancellationToken);

		public static Task<string> UpdateCollectionRelations(this IPermissionsRepository permissions, Guid collectionId, Update.Relation[] relations, CancellationToken cancellationToken)
			=> permissions.UpdateRelations("collection", collectionId, new Update { RelationsToUpdate = relations }, cancellationToken);



		public static IAsyncEnumerable<Guid> LookupNotes(this IPermissionsRepository permissions, ClaimsPrincipal user, CancellationToken cancellationToken)
			=> permissions.LookupResources("user", user.Id(), "read", "note", cancellationToken: cancellationToken);

		public static Task<bool> IsAllowedToReadNote(this IPermissionsRepository permissions, ClaimsPrincipal user, Guid noteId, CancellationToken cancellationToken)
			=> permissions.IsAllowed("user", user.Id(), "read", "note", noteId, cancellationToken: cancellationToken);

		public static Task<bool> IsAllowedToUpdateNote(this IPermissionsRepository permissions, ClaimsPrincipal user, Guid noteId, CancellationToken cancellationToken)
			=> permissions.IsAllowed("user", user.Id(), "update", "note", noteId, cancellationToken: cancellationToken);

		public static Task<bool> IsAllowedToUpdateNotePermissions(this IPermissionsRepository permissions, ClaimsPrincipal user, Guid collectionId, CancellationToken cancellationToken)
			=> permissions.IsAllowed("user", user.Id(), "change_permissions", "note", collectionId, cancellationToken: cancellationToken);

		public static Task<bool> IsAllowedToDeleteNote(this IPermissionsRepository permissions, ClaimsPrincipal user, Guid noteId, CancellationToken cancellationToken)
			=> permissions.IsAllowed("user", user.Id(), "delete", "note", noteId, cancellationToken: cancellationToken);

		public static Task RemoveNote(this IPermissionsRepository permissions, Guid noteId, CancellationToken cancellationToken)
			=> permissions.Remove("note", noteId, cancellationToken);

		public static Task<IReadOnlyCollection<IRelation>> GetNoteRelations(this IPermissionsRepository permissions, Guid noteId, CancellationToken cancellationToken)
			=> permissions.GetRelations("note", noteId, cancellationToken: cancellationToken);

		public static Task<string> UpdateNoteRelations(this IPermissionsRepository permissions, Guid noteId, Update.Relation[] relations, CancellationToken cancellationToken)
			=> permissions.UpdateRelations("note", noteId, new Update { RelationsToUpdate = relations }, cancellationToken);
	}
}
