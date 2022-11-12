using MediatR;
using U2Pa.Lib;

namespace U2pa.Electron.Link.Handlers.Prog
{
  public class ProgIdQuery : IRequest<string>
  {
    public class Handler : IRequestHandler<ProgIdQuery, string>
    {
      public async Task<string> Handle(ProgIdQuery request, CancellationToken cancellationToken)
      {
        var shouter = new Shouter(0);
        return await Task.FromResult(TopDevice.ReadTopDeviceIdString(shouter));
        //return EpromXml.Specified["2716"].ToString();
      }
    }
  }
}
