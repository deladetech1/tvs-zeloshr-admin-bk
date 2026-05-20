using ZelosHR.Api.Entities.Shared;

namespace ZelosHR.Api.Shared.Abstractions;

public interface IService<TReadDto, TWriteDto, TKey>
{
    Task<Respons<TReadDto>> GetByIdAsync(TKey id, CancellationToken ct = default);
    Task<Respons<IReadOnlyList<TReadDto>>> GetAllAsync(
        int page, int pageSize, CancellationToken ct = default);
    Task<Respons<TReadDto>> CreateAsync(TWriteDto dto, CancellationToken ct = default);
    Task<Respons<TReadDto>> UpdateAsync(TKey id, TWriteDto dto, CancellationToken ct = default);
    Task<Respons<bool>> DeleteAsync(TKey id, CancellationToken ct = default);
}
