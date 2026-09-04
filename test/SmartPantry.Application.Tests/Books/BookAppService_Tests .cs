using System;
using System.Linq;
using System.Threading.Tasks;
using Shouldly;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Modularity;
using Volo.Abp.Validation;
using Xunit;
using SmartPantry.Authors; // NUEVO: Importar el namespace de Autores

namespace SmartPantry.Books;

public abstract class BookAppService_Tests<TStartupModule> : SmartPantryApplicationTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{
    private readonly IBookAppService _bookAppService;
    private readonly IAuthorAppService _authorAppService; // NUEVO: Declarar el servicio de autores

    protected BookAppService_Tests()
    {
        _bookAppService = GetRequiredService<IBookAppService>();
        _authorAppService = GetRequiredService<IAuthorAppService>(); // NUEVO: Inyectar el servicio
    }

    [Fact]
    public async Task Should_Get_List_Of_Books()
    {
        //Act
        var result = await _bookAppService.GetListAsync(
            new PagedAndSortedResultRequestDto()
        );

        //Assert
        result.TotalCount.ShouldBeGreaterThan(0);
        result.Items.ShouldContain(b => b.Name == "1984");
    }

    [Fact]
    public async Task Should_Create_A_Valid_Book()
    {
        // NUEVO: Obtener la lista de autores de la base de datos de prueba
        // (Nota: Si tu DTO de consulta de autores se llama distinto, cambia GetAuthorListDto() por PagedAndSortedResultRequestDto())
        var authors = await _authorAppService.GetListAsync(new PagedAndSortedResultRequestDto());
        var firstAuthor = authors.Items.First(); // Tomar el primer autor disponible

        //Act
        var result = await _bookAppService.CreateAsync(
            new CreateUpdateBookDto
            {
                Name = "New test book 42",
                Price = 10,
                PublishDate = DateTime.Now,
                Type = BookType.ScienceFiction,
                AuthorId = firstAuthor.Id // NUEVO: Asignar el ID del autor
            }
        );

        //Assert
        result.Id.ShouldNotBe(Guid.Empty);
        result.Name.ShouldBe("New test book 42");
    }

    [Fact]
    public async Task Should_Not_Create_A_Book_Without_Name()
    {
        // NUEVO: También agregamos el AuthorId aquí para asegurar que la prueba falle 
        // estrictamente por la falta del 'Name' y no por la falta de 'AuthorId'
        var authors = await _authorAppService.GetListAsync(new PagedAndSortedResultRequestDto());
        var firstAuthor = authors.Items.First();

        var exception = await Assert.ThrowsAsync<AbpValidationException>(async () =>
        {
            await _bookAppService.CreateAsync(
                new CreateUpdateBookDto
                {
                    Name = "",
                    Price = 10,
                    PublishDate = DateTime.Now,
                    Type = BookType.ScienceFiction,
                    AuthorId = firstAuthor.Id // NUEVO: Asignar el ID del autor
                }
            );
        });

        exception.ValidationErrors
            .ShouldContain(err => err.MemberNames.Any(mem => mem == "Name"));
    }
}