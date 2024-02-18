using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Permissions;

namespace PermissionedNotes.Service
{
	[ApiController]
	[Route("notes")]
	[Authorize("notes")]
	public class NotesController : ControllerBase
	{
		private readonly ILogger<NotesController> logger;
		private readonly DB db;
		private readonly IPermissionsRepository permissions;

		public NotesController(ILogger<NotesController> logger, DB db, IPermissionsRepository permissions)
		{
			this.logger = logger;
			this.db = db;
			this.permissions = permissions;
		}

		[HttpGet]
		public async Task<IActionResult> ReadAll(CancellationToken cancellationToken)
		{
			var resources = await this.permissions.LookupNotes(User, cancellationToken).ToHashSetAsync(cancellationToken);

			var dtos = await db.Notes.AsNoTracking()
				.Where(n => resources.Contains(n.Id))
				.Select(n => new
				{
					n.Id,
					n.CreatedAt,
					n.UpdatedAt,
					collection = n.Collection != null ? (Guid?)n.Collection.Id : null,
				})
				.ToListAsync(cancellationToken);

			return Ok(new
			{
				notesCount = dtos.Count,
				notes = dtos
			});
		}

		[HttpGet]
		[Route("/collections/{id}/notes")]
		public async Task<IActionResult> ReadInCollection([FromRoute] Guid id, CancellationToken cancellationToken)
		{
			if (await this.permissions.IsAllowedToReadCollection(User, id, cancellationToken) != true)
				return Forbid();

			var collection = await db.Collections.AsNoTracking().SingleOrDefaultAsync(c => c.Id == id, cancellationToken);
			if (collection == null) return NotFound();

			var dtos = await db.Notes.AsNoTracking()
				.Where(n => n.Collection!.Id == collection.Id)
				.Select(n => new
				{
					n.Id,
					n.CreatedAt,
					n.CreatedBy,
					n.UpdatedAt,
					n.UpdatedBy,
					n.RowVersion,
					n.Text,
				})
				.ToListAsync(cancellationToken);

			return Ok(new
			{
				collection = new { collection.Id, collection.Name },
				notesCount = dtos.Count,
				notes = dtos
			});
		}

		[HttpGet]
		[Route("{id}")]
		public async Task<IActionResult> ReadOne([FromRoute] Guid id, CancellationToken cancellationToken)
		{
			if (await this.permissions.IsAllowedToReadNote(User, id, cancellationToken) != true)
				return Forbid();

			var note = await db.Notes.AsNoTracking().Include(n => n.Collection)
				.Where(n => n.Id == id)
				.SingleOrDefaultAsync(cancellationToken);

			if (note == null) return NoContent();

			var relations = await this.permissions.GetNoteRelations(id, cancellationToken);

			return Ok(new
			{
				note.Id,
				note.CreatedAt,
				note.CreatedBy,
				note.UpdatedAt,
				note.UpdatedBy,
				note.RowVersion,
				note.Text,
				collection = note.Collection != null ? new { note.Collection.Id, note.Collection.Name } : null,
				relations,
			});
		}

		[HttpPost]
		public async Task<IActionResult> Create([FromBody] NewNoteRequest newNote, CancellationToken cancellationToken)
		{
			var userId = User.Id();
			var noteId = Guid.NewGuid();

			await this.permissions.UpdateNoteRelations(
				noteId: noteId,
				relations: [new Update.Relation
				{
					SubjectType = "user",
					SubjectId = userId,
					Name = "owner",
					Operation = Permissions.Update.Operation.Touch,
				}],
				cancellationToken: cancellationToken);

			db.Notes.Add(new Note
			{
				Id = noteId,
				CreatedBy = userId,
				CreatedAt = DateTimeOffset.UtcNow,
				Text = newNote.Text,
			});
			await db.SaveChangesAsync(cancellationToken);

			return Created("notes", noteId);
		}
		public record NewNoteRequest(string Text);

		[HttpPost]
		[Route("/collections/{id}/notes")]
		public async Task<IActionResult> CreateInCollection([FromRoute] Guid id, [FromBody] NewNoteRequest newNote, CancellationToken cancellationToken)
		{
			if (await this.permissions.IsAllowedToCreateNote(User, id, cancellationToken) != true)
				return Forbid();

			var collection = await db.Collections.SingleOrDefaultAsync(c => c.Id == id, cancellationToken);
			if (collection == null) return NotFound();

			var noteId = Guid.NewGuid();

			await this.permissions.UpdateNoteRelations(
				noteId: noteId,
				relations: [new Update.Relation
				{
					SubjectType = "collection",
					SubjectId = collection.Id,
					Name = "parent",
					Operation = Permissions.Update.Operation.Touch
				}],
				cancellationToken: cancellationToken);

			db.Notes.Add(new Note
			{
				Id = noteId,
				CreatedBy = User.Id(),
				CreatedAt = DateTimeOffset.UtcNow,
				Text = newNote.Text,
				Collection = collection,
			});
			await db.SaveChangesAsync(cancellationToken);

			return Created("notes", noteId);
		}

		[HttpPut]
		[Route("{id}")]
		public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateNoteRequest updateNote, CancellationToken cancellationToken)
		{
			if (await this.permissions.IsAllowedToUpdateNote(User, id, cancellationToken) != true)
				return Forbid();

			var note = await db.Notes.SingleOrDefaultAsync(n => n.Id == id, cancellationToken);
			if (note == null) return NotFound();

			note.Text = updateNote.Text;
			note.UpdatedAt = DateTimeOffset.UtcNow;
			note.UpdatedBy = User.Id();
			db.Entry(note).Property(nameof(note.RowVersion)).OriginalValue = updateNote.RowVersion;
			await db.SaveChangesAsync(cancellationToken);

			return Ok();
		}
		public record UpdateNoteRequest(string Text, long RowVersion);

		[HttpPut]
		[Route("{id}/permissions")]
		public async Task<IActionResult> UpdatePermissions([FromRoute] Guid id, [FromBody] UpdateNotePermissionsRequest updateNote, CancellationToken cancellationToken)
		{
			if (await this.permissions.IsAllowedToUpdateNotePermissions(User, id, cancellationToken) != true)
				return Forbid();

			var note = await db.Notes.SingleOrDefaultAsync(n => n.Id == id, cancellationToken);
			if (note == null) return NotFound();

			await this.permissions.UpdateNoteRelations(id, updateNote.relations.ToArray(), cancellationToken);

			return Accepted();
		}
		public record UpdateNotePermissionsRequest(List<Update.Relation> relations);

		[HttpDelete]
		[Route("{id}")]
		public async Task<IActionResult> Delete([FromRoute] Guid id, CancellationToken cancellationToken)
		{
			if (await this.permissions.IsAllowedToDeleteNote(User, id, cancellationToken) != true)
				return Forbid();

			var note = await db.Notes.SingleOrDefaultAsync(n => n.Id == id, cancellationToken);
			if (note == null) return NotFound();

			await this.permissions.RemoveNote(id, cancellationToken);

			db.Notes.Remove(note);
			await db.SaveChangesAsync(cancellationToken);

			return NoContent();
		}
	}
}
