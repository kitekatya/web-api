using System.ComponentModel.DataAnnotations;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using NUnit.Framework.Internal;
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
    [Consumes("application/json")]
    public IActionResult CreateUser([FromBody] UserPostDto? user)
    {
        if (user == null)
            return BadRequest();

        if (string.IsNullOrEmpty(user.Login) || !user.Login.All(char.IsLetterOrDigit))
        {
            ModelState.AddModelError("login", "Invalid login format");
            return UnprocessableEntity(ModelState);
        }
        
        var userEntity = mapper.Map<UserEntity>(user);

        var createdUserEntity = userRepository.Insert(userEntity);
        return CreatedAtRoute(
            nameof(GetUserById),
            new { userId = createdUserEntity.Id },
            createdUserEntity.Id);
    }

    [HttpPut]
    [Consumes("application/json")]
    public IActionResult UpdateUser([FromBody] UserPutDto? user, [FromRoute] Guid userId)
    {
        if (user == null)
            return BadRequest();

        if (string.IsNullOrEmpty(user.Login) || !user.Login.All(char.IsLetterOrDigit))
        {
            ModelState.AddModelError("login", "Invalid login format");
            return UnprocessableEntity(ModelState);
        }
        
        var userEntity = mapper.Map<UserEntity>(user);

        userRepository.UpdateOrInsert();
    }
}