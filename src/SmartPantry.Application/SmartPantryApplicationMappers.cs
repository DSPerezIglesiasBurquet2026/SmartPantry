using Riok.Mapperly.Abstractions;
using System.Collections.Generic;
using Volo.Abp.Mapperly;
using SmartPantry.Authors;
using SmartPantry.Books;
using SmartPantry.Productos;

namespace SmartPantry;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class SmartPantryBookToBookDtoMapper : MapperBase<Book, BookDto>
{
    [MapperIgnoreTarget(nameof(BookDto.AuthorName))]
    public override partial BookDto Map(Book source);

    [MapperIgnoreTarget(nameof(BookDto.AuthorName))]
    public override partial void Map(Book source, BookDto destination);
}

[Mapper]
public partial class SmartPantryCreateUpdateBookDtoToBookMapper : MapperBase<CreateUpdateBookDto, Book>
{
    [MapperIgnoreTarget(nameof(Book.LastModificationTime))]
    [MapperIgnoreTarget(nameof(Book.LastModifierId))]
    [MapperIgnoreTarget(nameof(Book.CreationTime))]
    [MapperIgnoreTarget(nameof(Book.CreatorId))]
    [MapperIgnoreTarget(nameof(Book.ConcurrencyStamp))]
    public override partial Book Map(CreateUpdateBookDto source);

    [MapperIgnoreTarget(nameof(Book.LastModificationTime))]
    [MapperIgnoreTarget(nameof(Book.LastModifierId))]
    [MapperIgnoreTarget(nameof(Book.CreationTime))]
    [MapperIgnoreTarget(nameof(Book.CreatorId))]
    [MapperIgnoreTarget(nameof(Book.ConcurrencyStamp))]
    public override partial void Map(CreateUpdateBookDto source, Book destination);
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class SmartPantryAuthorToAuthorDtoMapper : MapperBase<Author, AuthorDto>
{
    public override partial AuthorDto Map(Author source);

    public override partial void Map(Author source, AuthorDto destination);
}

[Mapper]
public partial class SmartPantryCreateUpdateAuthorDtoToAuthorMapper : MapperBase<CreateUpdateAuthorDto, Author>
{
    [MapperIgnoreTarget(nameof(Author.IsDeleted))]
    [MapperIgnoreTarget(nameof(Author.DeleterId))]
    [MapperIgnoreTarget(nameof(Author.DeletionTime))]
    [MapperIgnoreTarget(nameof(Author.LastModificationTime))]
    [MapperIgnoreTarget(nameof(Author.LastModifierId))]
    [MapperIgnoreTarget(nameof(Author.CreationTime))]
    [MapperIgnoreTarget(nameof(Author.CreatorId))]
    [MapperIgnoreTarget(nameof(Author.ConcurrencyStamp))]
    public override partial Author Map(CreateUpdateAuthorDto source);

    [MapperIgnoreTarget(nameof(Author.IsDeleted))]
    [MapperIgnoreTarget(nameof(Author.DeleterId))]
    [MapperIgnoreTarget(nameof(Author.DeletionTime))]
    [MapperIgnoreTarget(nameof(Author.LastModificationTime))]
    [MapperIgnoreTarget(nameof(Author.LastModifierId))]
    [MapperIgnoreTarget(nameof(Author.CreationTime))]
    [MapperIgnoreTarget(nameof(Author.CreatorId))]
    [MapperIgnoreTarget(nameof(Author.ConcurrencyStamp))]
    public override partial void Map(CreateUpdateAuthorDto source, Author destination);
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class SmartPantryAuthorToAuthorExcelDtoMapper : MapperBase<Author, AuthorExcelDto>
{
    public override partial AuthorExcelDto Map(Author source);

    public override partial void Map(Author source, AuthorExcelDto destination);
}

// Mapeo registrado para TP05 / RF-08
[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class SmartPantryProductoToProductoDtoMapper : MapperBase<Producto, ProductoDto>
{
    public override partial ProductoDto Map(Producto source);

    public override partial void Map(Producto source, ProductoDto destination);
}

[Mapper]
public partial class ProductoToProductoDtoMapper
{
    // Este sí funciona perfecto porque solo lee propiedades de Producto 
    // e instancia un ProductoDto (que tiene setters públicos).
    public partial ProductoDto Map(Producto source);

    public partial List<ProductoDto> Map(List<Producto> source);
}