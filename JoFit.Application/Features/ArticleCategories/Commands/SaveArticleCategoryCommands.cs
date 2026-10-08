using JoFit.Application.Common.Interfaces;
using JoFit.Application.Features.ArticleCategories.Dtos;
using JoFit.Domain.ArticleCategories;
using JoFit.Domain.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JoFit.Application.Features.ArticleCategories.Commands;

public sealed record CreateArticleCategoryCommand(ArticleCategoryValues Values) : IRequest<Result<ArticleCategoryDto>>;

public sealed record UpdateArticleCategoryCommand(long Id, ArticleCategoryValues Values) : IRequest<Result<ArticleCategoryDto>>;

public sealed record DeleteArticleCategoryCommand(long Id) : IRequest<Result<Deleted>>;

public class CreateArticleCategoryCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<CreateArticleCategoryCommand, Result<ArticleCategoryDto>>
{
    public async Task<Result<ArticleCategoryDto>> Handle(CreateArticleCategoryCommand request, CancellationToken ct)
    {
        var created = ArticleCategory.Create(request.Values);
        if (created.IsError)
        {
            return created.Errors;
        }

        var entity = created.Value;
        var repository = unitOfWork.GetRepository<ArticleCategory>();
        var slugValue = entity.Slug;
        if (slugValue is not null && await repository.GetTableNoTracking().AnyAsync(x => x.Slug == slugValue, ct))
        {
            return ArticleCategoryErrors.SlugTaken;
        }

        await repository.AddAsync(entity, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return ArticleCategoryDto.From(entity);
    }
}

public class UpdateArticleCategoryCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateArticleCategoryCommand, Result<ArticleCategoryDto>>
{
    public async Task<Result<ArticleCategoryDto>> Handle(UpdateArticleCategoryCommand request, CancellationToken ct)
    {
        var repository = unitOfWork.GetRepository<ArticleCategory>();
        var entity = await repository.GetTableAsTracking().FirstOrDefaultAsync(x => x.Id == request.Id, ct);
        if (entity is null)
        {
            return ArticleCategoryErrors.NotFound;
        }

        var updated = entity.Update(request.Values);
        if (updated.IsError)
        {
            return updated.Errors;
        }
        var slugValue = entity.Slug;
        if (slugValue is not null && await repository.GetTableNoTracking().AnyAsync(x => x.Slug == slugValue && x.Id != entity.Id, ct))
        {
            return ArticleCategoryErrors.SlugTaken;
        }

        await unitOfWork.SaveChangesAsync(ct);
        return ArticleCategoryDto.From(entity);
    }
}

public class DeleteArticleCategoryCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteArticleCategoryCommand, Result<Deleted>>
{
    public async Task<Result<Deleted>> Handle(DeleteArticleCategoryCommand request, CancellationToken ct)
    {
        var repository = unitOfWork.GetRepository<ArticleCategory>();
        var entity = await repository.GetTableAsTracking().FirstOrDefaultAsync(x => x.Id == request.Id, ct);
        if (entity is null)
        {
            return ArticleCategoryErrors.NotFound;
        }

        var usedArticleCategoryLink = await unitOfWork.GetDbContext().Set<JoFit.Domain.Articles.ArticleCategoryLink>().CountAsync(x => x.ArticleCategoryId == entity.Id, ct);
        if (usedArticleCategoryLink > 0)
        {
            return Error.Conflict("ArticleCategory.InUse", $"This article category is used by {usedArticleCategoryLink} article(s). Move them first.");
        }

        // Records that reference this one add their own in-use check here when they exist.
        repository.DeleteAsync(entity);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Deleted;
    }
}
