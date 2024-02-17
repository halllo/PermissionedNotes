using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace PermissionedNotes.Service
{
	public class NotesDbContext : DbContext
	{
		public NotesDbContext(DbContextOptions<NotesDbContext> options) : base(options)
		{
		}

		public DbSet<Note> Notes { get; set; }
	}

	public class Note
	{
		public Guid NoteId { get; set; }
		public Guid CreatedBy { get; set; }
		public DateTimeOffset CreatedAt { get; set; }
		public Guid? UpdatedBy { get; set; }
		public DateTimeOffset? UpdatedAt { get; set; }

		[MaxLength(256)]
		public string Text { get; set; } = null!;

		[Timestamp]
		[Column("xmin", TypeName = "xid")]
		[DatabaseGenerated(DatabaseGeneratedOption.Computed)]
		public long RowVersion { get; set; }
	}
}
