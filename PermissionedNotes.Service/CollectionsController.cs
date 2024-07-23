using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Permissions;

namespace PermissionedNotes.Service
{
	[ApiController]
	[Route("collections")]
	[Authorize("notes")]
	public class CollectionsController : ControllerBase
	{
		private readonly ILogger<CollectionsController> logger;
		private readonly DB db;
		private readonly IPermissionsRepository permissions;

		public CollectionsController(ILogger<CollectionsController> logger, DB db, IPermissionsRepository permissions)
		{
			this.logger = logger;
			this.db = db;
			this.permissions = permissions;
		}

		[HttpGet]
		public async Task<IActionResult> ReadAll(CancellationToken cancellationToken)
		{
			var resources = await this.permissions.LookupCollections(User, cancellationToken).ToHashSetAsync(cancellationToken);

			var dtos = await db.Collections.AsNoTracking()
				.Where(c => resources.Contains(c.Id))
				.Select(c => new
				{
					c.Id,
					c.Name,
					c.CreatedAt,
					c.UpdatedAt,
				})
				.ToListAsync(cancellationToken);

			return Ok(new
			{
				collectionsCount = dtos.Count,
				collections = dtos
			});
		}

		[HttpGet]
		[Route("{id}")]
		public async Task<IActionResult> ReadOne([FromRoute] Guid id, CancellationToken cancellationToken)
		{
			if (await this.permissions.IsAllowedToReadCollection(User, id, cancellationToken) != true)
				return Forbid();

			var collection = await db.Collections.AsNoTracking()
				.Where(c => c.Id == id)
				.SingleOrDefaultAsync(cancellationToken);

			if (collection == null) return NoContent();

			var relations = await this.permissions.GetCollectionRelations(id, cancellationToken);

			return Ok(new
			{
				collection.Id,
				collection.Name,
				collection.CreatedAt,
				collection.CreatedBy,
				collection.UpdatedAt,
				collection.UpdatedBy,
				collection.RowVersion,
				relations,
			});
		}

		[HttpPost]
		public async Task<IActionResult> Create([FromBody] NewCollectionRequest newCollection, CancellationToken cancellationToken)
		{
			var userId = User.Id();
			var collectionId = Guid.NewGuid();

			await this.permissions.UpdateCollectionRelations(
				collectionId: collectionId,
				relations: [new Update.Relation
				{
					SubjectType = "user",
					SubjectId = userId,
					Name = "owner",
					Operation = Permissions.Update.Operation.Touch,
				}],
				cancellationToken: cancellationToken);

			db.Collections.Add(new Collection
			{
				Id = collectionId,
				Name = newCollection.Name,
				CreatedBy = userId,
				CreatedAt = DateTimeOffset.UtcNow,
			});
			await db.SaveChangesAsync(cancellationToken);

			return Created("collections", collectionId);
		}
		public record NewCollectionRequest(string Name);

		[HttpPut]
		[Route("{id}")]
		public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateCollectionRequest updateCollection, CancellationToken cancellationToken)
		{
			if (await this.permissions.IsAllowedToUpdateCollection(User, id, cancellationToken) != true)
				return Forbid();

			var collection = await db.Collections.SingleOrDefaultAsync(c => c.Id == id, cancellationToken);
			if (collection == null) return NotFound();

			collection.Name = updateCollection.Name;
			collection.UpdatedAt = DateTimeOffset.UtcNow;
			collection.UpdatedBy = User.Id();
			db.Entry(collection).Property(nameof(collection.RowVersion)).OriginalValue = updateCollection.RowVersion;
			await db.SaveChangesAsync(cancellationToken);

			return Ok();
		}
		public record UpdateCollectionRequest(string Name, long RowVersion);

		[HttpPut]
		[Route("{id}/permissions")]
		public async Task<IActionResult> UpdatePermissions([FromRoute] Guid id, [FromBody] UpdateCollectionPermissionsRequest updateCollection, CancellationToken cancellationToken)
		{
			if (await this.permissions.IsAllowedToUpdateCollectionPermissions(User, id, cancellationToken) != true)
				return Forbid();

			var collection = await db.Collections.SingleOrDefaultAsync(c => c.Id == id, cancellationToken);
			if (collection == null) return NotFound();

			await this.permissions.UpdateCollectionRelations(id, updateCollection.relations.ToArray(), cancellationToken);

			return Accepted();
		}
		public record UpdateCollectionPermissionsRequest(List<Update.Relation> relations);

		[HttpDelete]
		[Route("{id}")]
		public async Task<IActionResult> Delete([FromRoute] Guid id, CancellationToken cancellationToken)
		{
			if (await this.permissions.IsAllowedToDeleteCollection(User, id, cancellationToken) != true)
				return Forbid();

			var collection = await this.db.Collections.Include(c => c.Notes).SingleOrDefaultAsync(n => n.Id == id, cancellationToken);
			if (collection == null) return NotFound();

			// beware multiple transactions (spicedb & postgres)
			foreach	(var note in collection.Notes)
			{
				await this.permissions.RemoveNote(note.Id, cancellationToken);
				this.db.Notes.Remove(note);
			}

			await this.permissions.RemoveCollection(id, cancellationToken);
			this.db.Collections.Remove(collection);

			await this.db.SaveChangesAsync(cancellationToken);

			return NoContent();
		}
	}
}
