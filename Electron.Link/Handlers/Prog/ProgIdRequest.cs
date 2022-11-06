using MediatR;
using U2Pa.Lib;
using U2Pa.Lib.IC;

namespace U2pa.Electron.Link.Handlers.Prog
{
  public class ProgIdRequest : IRequest<string>
  {
    public class Handler : IRequestHandler<ProgIdRequest, string>
    {
      public async Task<string> Handle(ProgIdRequest request, CancellationToken cancellationToken)
      {
        var shouter = new Shouter(0);
        return await Task.FromResult(TopDevice.ReadTopDeviceIdString(shouter));
        //return EpromXml.Specified["2716"].ToString();
      }
    }
  }
}
