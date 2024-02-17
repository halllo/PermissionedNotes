using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace PermissionedNotes.Service
{
	[ApiController]
	[Route("notes")]
	[Authorize("notes")]
	public class NotesController : ControllerBase
	{
		private readonly ILogger<NotesController> logger;
		private readonly NotesDbContext db;

		public NotesController(ILogger<NotesController> logger, NotesDbContext db)
		{
			this.logger = logger;
			this.db = db;
		}

		[HttpGet]
		public async Task<IActionResult> ReadAll(CancellationToken cancellationToken)
		{
			var userId = User.Id();
			var dtos = await db.Notes.AsNoTracking()
				.Select(n => new
				{
					n.NoteId,
					n.CreatedAt,
					n.UpdatedAt,
				})
				.ToListAsync(cancellationToken);
			return Ok(dtos);
		}

		[HttpGet]
		[Route("{id}")]
		public async Task<IActionResult> ReadOne([FromRoute] Guid id, CancellationToken cancellationToken)
		{
			var userId = User.Id();
			var dto = await db.Notes.AsNoTracking()
				.Where(n => n.NoteId == id)
				.Select(n => new
				{
					n.NoteId,
					n.CreatedAt,
					n.CreatedBy,
					n.UpdatedAt,
					n.UpdatedBy,
					n.RowVersion,
					n.Text,
				})
				.SingleOrDefaultAsync(cancellationToken);

			return Ok(dto);
		}

		[HttpPost]
		public async Task<IActionResult> Create([FromBody]NewNoteRequest newNote, CancellationToken cancellationToken)
		{
			var userId = User.Id();
			var noteId = Guid.NewGuid();

			db.Notes.Add(new Note
			{
				NoteId = noteId,
				CreatedBy = userId,
				CreatedAt = DateTimeOffset.UtcNow,
				Text = newNote.Text,
			});
			await db.SaveChangesAsync();

			return Created("notes", noteId);
		}
		public record NewNoteRequest(string Text);

		[HttpPut]
		[Route("{id}")]
		public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody]UpdateNoteRequest updateNote, CancellationToken cancellationToken)
		{
			var userId = User.Id();
			var note = await db.Notes.SingleOrDefaultAsync(n => n.NoteId == id, cancellationToken);
			if (note == null) return NotFound();

			note.Text = updateNote.Text;
			note.UpdatedAt = DateTimeOffset.UtcNow;
			note.UpdatedBy = userId;
			db.Entry(note).Property(nameof(note.RowVersion)).OriginalValue = updateNote.RowVersion;
			await db.SaveChangesAsync();

			return Ok();
		}
		public record UpdateNoteRequest(string Text, long RowVersion);

		[HttpDelete]
		[Route("{id}")]
		public async Task<IActionResult> Delete([FromRoute] Guid id, CancellationToken cancellationToken)
		{
			var userId = User.Id();
			var note = await db.Notes.SingleOrDefaultAsync(n => n.NoteId == id, cancellationToken);
			if (note == null) return NotFound();
			
			db.Notes.Remove(note);
			await db.SaveChangesAsync();

			return NoContent();
		}
	}
}
