using MediatR;
using U2Pa.Lib;
using U2Pa.Lib.IC;

namespace U2pa.Electron.Link.Handlers.Rom
{
  public class ReadCommand : IRequest<bool>
  {
    public GuiState State { get; set; }
    public string RomType { get; set; }

    public ReadCommand(GuiState guiState, string romType)
    {
      State = guiState;
      RomType = romType;
    }

    public class Handler : IRequestHandler<ReadCommand, bool>
    {
      public async Task<bool> Handle(ReadCommand request, CancellationToken cancellationToken)
      {
        var shouter = new GuiShouter(request.State);
        using (var progressBar = new GuiProgressBar(request.State))
        {
          progressBar.Shout("Initializing ...");
          try
          {
            await Task<IList<byte>>.Run(() =>
            {
              IList<byte> bytes = new List<byte>();
              var eprom = EpromXml.Specified[request.RomType];
              var totalNumberOfAdresses = eprom.AddressPins.Length == 0 ? 0 : 1 << eprom.AddressPins.Length;
              var startAddress = 0;
              while (startAddress < totalNumberOfAdresses)
              {
                using (var topDevice = TopDevice.Create(shouter))
                {
                  progressBar.Shout("Reading");
                  startAddress = topDevice.ReadEprom(eprom, progressBar, bytes, startAddress, totalNumberOfAdresses, cancellationToken);
                }
                if (startAddress < totalNumberOfAdresses)
                  progressBar.Shout("Disposing Top USB interface and inits a new");
              }
              progressBar.Shout("Done!");
            }, cancellationToken);
          }
          catch(TaskCanceledException)
          {
            progressBar.Shout("Canceled!");
          }
          catch (OperationCanceledException)
          {
            progressBar.Shout("Canceled!");
          }
        }
        return true;
      }
    }
  }
}
