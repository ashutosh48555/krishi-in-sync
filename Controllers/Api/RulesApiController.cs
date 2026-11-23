using AgriSyncIndia.Data;
using AgriSyncIndia.Models;
using AgriSyncIndia.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace AgriSyncIndia.Controllers.Api;

public class RuleCreateDto
{
    [Required]
    [StringLength(50)]
    public string ConditionVariable { get; set; } = "Temperature";

    [Required]
    [StringLength(10)]
    public string Operator { get; set; } = ">";

    [Required]
    public double ThresholdValue { get; set; }

    [Required]
    [StringLength(100)]
    public string ActionTarget { get; set; } = "Turn ON Exhaust Fans";

    public bool IsActive { get; set; } = true;

    [StringLength(200)]
    public string? Description { get; set; }
}

public class RuleUpdateDto
{
    public string? ConditionVariable { get; set; }
    public string? Operator { get; set; }
    public double? ThresholdValue { get; set; }
    public string? ActionTarget { get; set; }
    public bool? IsActive { get; set; }
    public string? Description { get; set; }
}

/// <summary>
/// RESTful Web API for Automation Rules CRUD operations (CO6).
/// Testable via Postman or curl.
/// </summary>
[ApiController]
[Route("api/rules")]
[Produces("application/json")]
public class RulesApiController : ControllerBase
{
    private readonly AgriSyncDbContext _context;

    public RulesApiController(AgriSyncDbContext context)
    {
        _context = context;
    }

    /// <summary>GET /api/rules - List all automation rules.</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AutomationRule>>> GetRules()
    {
        return await _context.AutomationRules.AsNoTracking()
            .OrderByDescending(r => r.RuleId)
            .ToListAsync();
    }

    /// <summary>GET /api/rules/{id} - Get a single rule by ID.</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<AutomationRule>> GetRule(int id)
    {
        var rule = await _context.AutomationRules.FindAsync(id);
        if (rule == null)
            return NotFound(new { error = $"Automation rule #{id} not found." });
        return rule;
    }

    /// <summary>POST /api/rules - Create a new automation rule.</summary>
    [HttpPost]
    public async Task<ActionResult<AutomationRule>> CreateRule([FromBody] RuleCreateDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var rule = new AutomationRule
        {
            ConditionVariable = dto.ConditionVariable,
            Operator = dto.Operator,
            ThresholdValue = dto.ThresholdValue,
            ActionTarget = dto.ActionTarget,
            IsActive = dto.IsActive,
            Description = dto.Description,
            CreatedAt = IstClock.Now
        };

        _context.AutomationRules.Add(rule);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetRule), new { id = rule.RuleId }, rule);
    }

    /// <summary>PUT /api/rules/{id} - Update an existing automation rule.</summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateRule(int id, [FromBody] RuleUpdateDto dto)
    {
        var rule = await _context.AutomationRules.FindAsync(id);
        if (rule == null)
            return NotFound(new { error = $"Automation rule #{id} not found." });

        if (dto.ConditionVariable != null) rule.ConditionVariable = dto.ConditionVariable;
        if (dto.Operator != null) rule.Operator = dto.Operator;
        if (dto.ThresholdValue.HasValue) rule.ThresholdValue = dto.ThresholdValue.Value;
        if (dto.ActionTarget != null) rule.ActionTarget = dto.ActionTarget;
        if (dto.IsActive.HasValue) rule.IsActive = dto.IsActive.Value;
        if (dto.Description != null) rule.Description = dto.Description;

        await _context.SaveChangesAsync();
        return Ok(rule);
    }

    /// <summary>DELETE /api/rules/{id} - Delete an automation rule.</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteRule(int id)
    {
        var rule = await _context.AutomationRules.FindAsync(id);
        if (rule == null)
            return NotFound(new { error = $"Automation rule #{id} not found." });

        _context.AutomationRules.Remove(rule);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
