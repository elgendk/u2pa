using MediatR;
using U2Pa.Lib.IC;

namespace U2pa.Electron.Link.Handlers.Rom
{
  public class GetAllRomTypesQuery : IRequest<List<string>>
  {
    public class Handler : IRequestHandler<GetAllRomTypesQuery, List<string>>
    {
      public Task<List<string>> Handle(GetAllRomTypesQuery request, CancellationToken cancellationToken)
      {
        return Task.FromResult(EpromXml.Specified.Keys.OrderBy(x => x.PadLeft(10, '0')).ToList());
      }
    }
  }
}
