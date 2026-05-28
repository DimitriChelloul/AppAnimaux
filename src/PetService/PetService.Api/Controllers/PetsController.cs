using Microsoft.AspNetCore.Mvc;
using PetService.BLL.Models;
using PetService.BLL.Services;

namespace PetService.Api.Controllers;

[ApiController]
[Route("pets")]
public sealed class PetsController : ControllerBase
{
    private readonly IPetAppService _pets;

    public PetsController(IPetAppService pets) => _pets = pets;

    [HttpGet("mine")]
    public async Task<IActionResult> GetMine(CancellationToken ct)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(new { message = "Missing X-User-Id header." });
        }

        return Ok(await _pets.GetMineAsync(userId, ct));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePetRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(new { message = "Missing X-User-Id header." });
        }

        try
        {
            var pet = await _pets.CreateAsync(userId, request, ct);
            return CreatedAtAction(nameof(Get), new { id = pet.Pet.Id }, pet);
        }
        catch (ArgumentException ex)
        {
            return ValidationProblem(ex.Message);
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(new { message = "Missing X-User-Id header." });
        }

        var pet = await _pets.GetAsync(userId, id, ct);
        return pet is null ? NotFound() : Ok(pet);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePetRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(new { message = "Missing X-User-Id header." });
        }

        try
        {
            var pet = await _pets.UpdateAsync(userId, id, request, ct);
            return pet is null ? NotFound() : Ok(pet);
        }
        catch (ArgumentException ex)
        {
            return ValidationProblem(ex.Message);
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(new { message = "Missing X-User-Id header." });
        }

        return await _pets.DeleteAsync(userId, id, ct) ? NoContent() : NotFound();
    }

    [HttpPost("{id:guid}/photos")]
    public async Task<IActionResult> AddPhoto(Guid id, [FromBody] PetPhotoRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(new { message = "Missing X-User-Id header." });
        }

        var photo = await _pets.AddPhotoAsync(userId, id, request, ct);
        return photo is null ? NotFound() : Ok(photo);
    }

    [HttpPut("{id:guid}/main-photo")]
    public async Task<IActionResult> SetMainPhoto(Guid id, [FromBody] SetMainPhotoRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(new { message = "Missing X-User-Id header." });
        }

        return await _pets.SetMainPhotoAsync(userId, id, request, ct) ? NoContent() : NotFound();
    }

    private bool TryGetUserId(out Guid userId)
    {
        var value = Request.Headers["X-User-Id"].ToString();
        return Guid.TryParse(value, out userId);
    }
}
