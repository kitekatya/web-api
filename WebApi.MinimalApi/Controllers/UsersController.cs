using AutoMapper;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using WebApi.MinimalApi.Domain;
using WebApi.MinimalApi.Models;

namespace WebApi.MinimalApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UsersController : Controller
{
    private readonly IUserRepository userRepository;
    private readonly IMapper mapper;
    // Чтобы ASP.NET положил что-то в userRepository требуется конфигурация

    public UsersController(IUserRepository userRepository, IMapper mapper)
    {
        this.mapper = mapper;
        this.userRepository = userRepository;
    }

    [Produces("application/json", "application/xml")]
    [HttpGet("{userId}", Name = nameof(GetUserById))]
    public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
    {
        var user = userRepository.FindById(userId);
        if (user is null)
            return NotFound();
        var userDto = mapper.Map<UserDto>(user);
        return Ok(userDto);
    }


    [HttpPost]
    [Produces("application/json", "application/xml")]
    public IActionResult CreateUser([FromBody] UserPostDto? user)
    {
        if (user == null)
            return BadRequest();

        Validate(user);
        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);
        
        var userEntity = mapper.Map<UserEntity>(user);

        var createdUserEntity = userRepository.Insert(userEntity);
        return CreatedAtRoute(
            nameof(GetUserById),
            new { userId = createdUserEntity.Id },
            createdUserEntity.Id);
    }

    [HttpPut("{userId}")]
    [Produces("application/json", "application/xml")]
    public IActionResult UpdateUser([FromBody] UserUpdateDto? user, [FromRoute] Guid userId)
    {
        if (user == null || userId == Guid.Empty)
            return BadRequest();

        Validate(user);
        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);
        
        var userEntity = mapper.Map(user, new UserEntity(userId));
        userRepository.UpdateOrInsert(userEntity, out var isInserted);
        if (isInserted)
        {
            return CreatedAtAction(
                actionName: nameof(GetUserById),
                routeValues: new { userId },
                value: userId);
        }

        return NoContent();
    }
    
    [HttpPatch("{userId:guid}")]
    [Produces("application/json", "application/xml")]
    public IActionResult PartialUpdateUser([FromRoute] Guid userId, [FromBody] JsonPatchDocument<UserUpdateDto> patchDoc)
    {
        if (patchDoc == null)
            return BadRequest();

        var existingUser = userRepository.FindById(userId);
        if (existingUser == null || userId == Guid.Empty)
            return NotFound();
        
        var userToUpdate = mapper.Map<UserUpdateDto>(existingUser);
        patchDoc.ApplyTo(userToUpdate, ModelState);
        Validate(userToUpdate);
        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);

        var entity = mapper.Map<UserEntity>(userToUpdate);
        userRepository.Update(entity);
        
        return NoContent();
    }

    [HttpDelete("{userId:guid}")]
    [Produces("application/json", "application/xml")]
    public IActionResult DeleteUser([FromRoute] Guid userId)
    {
        if (userId == Guid.Empty)
            return BadRequest();
        if (userRepository.FindById(userId) is null)
            return NotFound();
        
        userRepository.Delete(userId);
        return NoContent();
    }
    
    private void Validate(UserUpdateDto user)
    {
        if (string.IsNullOrEmpty(user.Login) || !user.Login.All(char.IsLetterOrDigit))
            ModelState.AddModelError("login", "Invalid login format");
        
        if (string.IsNullOrEmpty(user.FirstName))
            ModelState.AddModelError("firstName", "Invalid name format");
        
        if (string.IsNullOrEmpty(user.LastName))
            ModelState.AddModelError("lastName", "Invalid name format");
    }

    private void Validate(UserPostDto user)
    {
        if (string.IsNullOrEmpty(user.Login) || !user.Login.All(char.IsLetterOrDigit))
            ModelState.AddModelError("login", "Invalid login format");
    }
}