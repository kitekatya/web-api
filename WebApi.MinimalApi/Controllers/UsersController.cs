using AutoMapper;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using WebApi.MinimalApi.Domain;
using WebApi.MinimalApi.Models;

namespace WebApi.MinimalApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UsersController : Controller
{
    private readonly IUserRepository userRepository;
    private readonly IMapper mapper;
    private readonly LinkGenerator linkGenerator;

    public UsersController(IUserRepository userRepository, IMapper mapper, LinkGenerator linkGenerator)
    {
        this.mapper = mapper;
        this.userRepository = userRepository;
        this.linkGenerator = linkGenerator;
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
    public IActionResult CreateUser([FromBody] UserCreateDto? user)
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
            return CreatedAtAction(
                actionName: nameof(GetUserById),
                routeValues: new { userId },
                value: userId);

        return NoContent();
    }
    
    [HttpPatch("{userId}")]
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
    
    [HttpHead("{userId}")]
    [Produces("application/json")]
    public IActionResult HeadUserById([FromRoute] Guid userId)
    {
        var user = userRepository.FindById(userId);
        if (user is null)
            return NotFound();

        Response.ContentType = "application/json; charset=utf-8";
        return Ok();
    }

    [HttpGet(Name = "GetUsers")]
    [Produces("application/json", "application/xml")]
    public IActionResult GetUsers([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        pageNumber = Math.Max(pageNumber, 1);
        pageSize = Math.Max(pageSize, 1);
        pageSize = Math.Min(pageSize, 20);

        var pageList = userRepository.GetPage(pageNumber, pageSize);
        var totalCount = userRepository.GetTotalCount();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        var previousPageLink = pageNumber > 1
            ? linkGenerator.GetUriByRouteValues(
                HttpContext,
                routeName: "GetUsers",
                values: new { pageNumber = pageNumber - 1, pageSize })
            : null;

        var nextPageLink = linkGenerator.GetUriByRouteValues(HttpContext,
            routeName: "GetUsers",
            values: new { pageNumber = pageNumber + 1, pageSize });

        var paginationHeader = new
        {
            previousPageLink = previousPageLink,
            nextPageLink = nextPageLink,
            pageSize = pageSize,
            currentPage = pageNumber,
            totalCount = totalCount,
            totalPages = totalPages
        };
        Response.Headers.Add("X-Pagination", JsonConvert.SerializeObject(paginationHeader));
        var users = mapper.Map<IEnumerable<UserDto>>(pageList);
        return Ok(users);
    }
    
    [HttpOptions]
    public IActionResult GetOptions()
    {
        Response.Headers.Append("Allow", "GET, POST, OPTIONS");
        return Ok();
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

    private void Validate(UserCreateDto user)
    {
        if (string.IsNullOrEmpty(user.Login) || !user.Login.All(char.IsLetterOrDigit))
            ModelState.AddModelError("login", "Invalid login format");
    }
}