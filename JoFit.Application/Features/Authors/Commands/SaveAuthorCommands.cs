using JoFit.Application.Common.Interfaces;
using JoFit.Application.Features.Authors.Dtos;
using JoFit.Domain.Authors;
using JoFit.Domain.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JoFit.Application.Features.Authors.Commands;

public sealed record CreateAuthorCommand(AuthorValues Values) : IRequest<Result<AuthorDto>>;

public sealed record UpdateAuthorCommand(long Id, AuthorValues Values) : IRequest<Result<AuthorDto>>;

public sealed record DeleteAuthorCommand(long Id) : IRequest<Result<Deleted>>;

public class CreateAuthorCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<CreateAuthorCommand, Result<AuthorDto>>
{
    public async Task<Result<AuthorDto>> Handle(CreateAuthorCommand request, CancellationToken ct)
    {
        var created = Author.Create(request.Values);
        if (created.IsError)
        {
            return created.Errors;
        }

        var entity = created.Value;
        var repository = unitOfWork.GetRepository<Author>();
        var slugValue = entity.Slug;
        if (slugValue is not null && await repository.GetTableNoTracking().AnyAsync(x => x.Slug == slugValue, ct))
        {
            return AuthorErrors.SlugTaken;
        }

        await repository.AddAsync(entity, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return AuthorDto.From(entity);
    }
}

public class UpdateAuthorCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateAuthorCommand, Result<AuthorDto>>
{
    public async Task<Result<AuthorDto>> Handle(UpdateAuthorCommand request, CancellationToken ct)
    {
        var repository = unitOfWork.GetRepository<Author>();
        var entity = await repository.GetTableAsTracking().FirstOrDefaultAsync(x => x.Id == request.Id, ct);
        if (entity is null)
        {
            return AuthorErrors.NotFound;
        }

        var updated = entity.Update(request.Values);
        if (updated.IsError)
        {
            return updated.Errors;
        }
        var slugValue = entity.Slug;
        if (slugValue is not null && await repository.GetTableNoTracking().AnyAsync(x => x.Slug == slugValue && x.Id != entity.Id, ct))
        {
            return AuthorErrors.SlugTaken;
        }

        await unitOfWork.SaveChangesAsync(ct);
        return AuthorDto.From(entity);
    }
}

public class DeleteAuthorCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteAuthorCommand, Result<Deleted>>
{
    public async Task<Result<Deleted>> Handle(DeleteAuthorCommand request, CancellationToken ct)
    {
        var repository = unitOfWork.GetRepository<Author>();
        var entity = await repository.GetTableAsTracking().FirstOrDefaultAsync(x => x.Id == request.Id, ct);
        if (entity is null)
        {
            return AuthorErrors.NotFound;
        }

        var usedArticle = await unitOfWork.GetDbContext().Set<JoFit.Domain.Articles.Article>().CountAsync(x => x.AuthorId == entity.Id, ct);
        if (usedArticle > 0)
        {
            return Error.Conflict("Author.InUse", $"This author is used by {usedArticle} article(s). Move them first.");
        }

        // Records that reference this one add their own in-use check here when they exist.
        repository.DeleteAsync(entity);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Deleted;
    }
}
