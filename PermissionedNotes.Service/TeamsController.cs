using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Permissions;

namespace PermissionedNotes.Service
{
	[ApiController]
	[Route("teams")]
	[Authorize("admin")]
	public class TeamsController : ControllerBase
	{
		private readonly ILogger<TeamsController> logger;
		private readonly DB db;
		private readonly IPermissionsRepository permissions;

		public TeamsController(ILogger<TeamsController> logger, DB db, IPermissionsRepository permissions)
		{
			this.logger = logger;
			this.db = db;
			this.permissions = permissions;
		}

		[HttpGet]
		public async Task<IActionResult> ReadAll(CancellationToken cancellationToken)
		{
			var dtos = await db.Teams.AsNoTracking()
				.Select(t => new
				{
					t.Id,
					t.Name,
				})
				.ToListAsync(cancellationToken);

			return Ok(new
			{
				teamsCount = dtos.Count,
				teams = dtos
			});
		}

		[HttpGet]
		[Route("{id}")]
		public async Task<IActionResult> ReadOne([FromRoute] Guid id, CancellationToken cancellationToken)
		{
			var team = await db.Teams.AsNoTracking()
				.Where(t => t.Id == id)
				.SingleOrDefaultAsync(cancellationToken);

			if (team == null) return NoContent();

			var relations = await this.permissions.GetTeamRelations(id, cancellationToken);

			return Ok(new
			{
				team.Id,
				team.CreatedAt,
				team.CreatedBy,
				team.UpdatedAt,
				team.UpdatedBy,
				team.RowVersion,
				team.Name,
				members = relations
				.Where(r => r.Relation == "member")
				.Select(r => r.SubjectId)
				.ToArray()
			});
		}

		[HttpPost]
		public async Task<IActionResult> Create([FromBody] NewTeamRequest newTeam, CancellationToken cancellationToken)
		{
			var userId = User.Id();
			var teamId = Guid.NewGuid();

			db.Teams.Add(new Team
			{
				Id = teamId,
				CreatedBy = userId,
				CreatedAt = DateTimeOffset.UtcNow,
				Name = newTeam.Name,
			});
			await db.SaveChangesAsync(cancellationToken);

			return Created("teams", teamId);
		}
		public record NewTeamRequest(string Name);

		[HttpPut]
		[Route("{id}")]
		public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateTeamRequest updateTeam, CancellationToken cancellationToken)
		{
			var team = await db.Teams.SingleOrDefaultAsync(t => t.Id == id, cancellationToken);
			if (team == null) return NotFound();

			team.Name = updateTeam.Name;
			team.UpdatedAt = DateTimeOffset.UtcNow;
			team.UpdatedBy = User.Id();
			db.Entry(team).Property(nameof(team.RowVersion)).OriginalValue = updateTeam.RowVersion;
			await db.SaveChangesAsync(cancellationToken);

			return Ok();
		}
		public record UpdateTeamRequest(string Name, long RowVersion);

		[HttpPut]
		[Route("{id}/members")]
		public async Task<IActionResult> UpdatePermissions([FromRoute] Guid id, [FromBody] UpdateTeamMembersRequest updateTeam, CancellationToken cancellationToken)
		{
			var team = await db.Teams.SingleOrDefaultAsync(t => t.Id == id, cancellationToken);
			if (team == null) return NotFound();

			var updateRelations = Enumerable.Concat(
				updateTeam.AddUsers.Select(userId => new Update.Relation
				{
					SubjectType = "user",
					SubjectId = userId,
					Name = "member",
					Operation = Permissions.Update.Operation.Touch,
				}),
				updateTeam.RemoveUsers.Select(userId => new Update.Relation
				{
					SubjectType = "user",
					SubjectId = userId,
					Name = "member",
					Operation = Permissions.Update.Operation.Remove,
				})
				).ToArray();

			await this.permissions.UpdateTeamRelations(id, updateRelations, cancellationToken);

			return Accepted();
		}
		public record UpdateTeamMembersRequest(List<Guid> AddUsers, List<Guid> RemoveUsers);

		[HttpDelete]
		[Route("{id}")]
		public async Task<IActionResult> Delete([FromRoute] Guid id, CancellationToken cancellationToken)
		{
			var team = await db.Teams.SingleOrDefaultAsync(t => t.Id == id, cancellationToken);
			if (team == null) return NotFound();

			await this.permissions.RemoveTeam(id, cancellationToken);

			db.Teams.Remove(team);
			await db.SaveChangesAsync(cancellationToken);

			return NoContent();
		}
	}
}
