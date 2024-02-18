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

			var dto = await db.Collections.AsNoTracking()
				.Where(c => c.Id == id)
				.Select(c => new
				{
					c.Id,
					c.Name,
					c.CreatedAt,
					c.CreatedBy,
					c.UpdatedAt,
					c.UpdatedBy,
					c.RowVersion,
				})
				.SingleOrDefaultAsync(cancellationToken);

			return Ok(dto);
		}

		[HttpPost]
		public async Task<IActionResult> Create([FromBody] NewCollectionRequest newCollection, CancellationToken cancellationToken)
		{
			var userId = User.Id();
			var collectionId = Guid.NewGuid();

			await this.permissions.UpdateRelations("collection", collectionId, new Update
			{
				RelationsToUpdate = [new Update.Relation { SubjectType = "user", SubjectId = userId, Name = "owner", Operation = Permissions.Update.Operation.Touch, }]
			}, cancellationToken);

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

		[HttpDelete]
		[Route("{id}")]
		public async Task<IActionResult> Delete([FromRoute] Guid id, CancellationToken cancellationToken)
		{
			if (await this.permissions.IsAllowedToDeleteCollection(User, id, cancellationToken) != true)
				return Forbid();

			var collection = await db.Collections.SingleOrDefaultAsync(n => n.Id == id, cancellationToken);
			if (collection == null) return NotFound();

			await this.permissions.RemoveCollection(id, cancellationToken);

			db.Collections.Remove(collection);
			await db.SaveChangesAsync(cancellationToken);

			return NoContent();
		}
	}
}
