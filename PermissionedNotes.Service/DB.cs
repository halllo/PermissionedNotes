using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace PermissionedNotes.Service
{
	public class DB : DbContext
	{
		public DB(DbContextOptions<DB> options) : base(options)
		{
		}

		public DbSet<Note> Notes { get; set; }
		public DbSet<Collection> Collections { get; set; }
		public DbSet<Team> Teams { get; set; }
	}

	public class Note
	{
		public Guid Id { get; set; }
		public Guid CreatedBy { get; set; }
		public DateTimeOffset CreatedAt { get; set; }
		public Guid? UpdatedBy { get; set; }
		public DateTimeOffset? UpdatedAt { get; set; }
		public Collection? Collection { get; set; }

		public string Text { get; set; } = null!;

		[Timestamp]
		[Column("xmin", TypeName = "xid")]
		[DatabaseGenerated(DatabaseGeneratedOption.Computed)]
		public long RowVersion { get; set; }
	}

	public class Collection
	{
		public Guid Id { get; set; }
		public Guid CreatedBy { get; set; }
		public DateTimeOffset CreatedAt { get; set; }
		public Guid? UpdatedBy { get; set; }
		public DateTimeOffset? UpdatedAt { get; set; }
		public IList<Note> Notes { get; set; } = null!;

		[MaxLength(256)]
		public string Name { get; set; } = null!;

		[Timestamp]
		[Column("xmin", TypeName = "xid")]
		[DatabaseGenerated(DatabaseGeneratedOption.Computed)]
		public long RowVersion { get; set; }
	}

	public class Team
	{
		public Guid Id { get; set; }
		public Guid CreatedBy { get; set; }
		public DateTimeOffset CreatedAt { get; set; }
		public Guid? UpdatedBy { get; set; }
		public DateTimeOffset? UpdatedAt { get; set; }
		
		[MaxLength(256)]
		public string Name { get; set; } = null!;

		[Timestamp]
		[Column("xmin", TypeName = "xid")]
		[DatabaseGenerated(DatabaseGeneratedOption.Computed)]
		public long RowVersion { get; set; }
	}
}
