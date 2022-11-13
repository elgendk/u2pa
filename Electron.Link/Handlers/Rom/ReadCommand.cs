using MediatR;
using U2Pa.Lib;
using U2Pa.Lib.IC;

namespace U2pa.Electron.Link.Handlers.Rom
{
  public class ReadCommand : IRequest<bool>
  {
    public GuiState State { get; set; }
    public string RomType { get; set; }
    public string FileName { get; set; }

    public ReadCommand(GuiState guiState, string romType, string fileName)
    {
      State = guiState;
      RomType = romType;
      FileName = fileName;
    }

    public class Handler : IRequestHandler<ReadCommand, bool>
    {
      public async Task<bool> Handle(ReadCommand request, CancellationToken cancellationToken)
      {
        var shouter = new GuiShouter(request.State);
        using (var progressBar = new GuiProgressBar(request.State))
        {
          IList<byte> bytes = new List<byte>();
          progressBar.Shout("Initializing ...");
          try
          {
            bytes = await Task<IList<byte>>.Run(() =>
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
              progressBar.Shout("Done reading");
              return bytes;
            }, cancellationToken);
          }
          catch(TaskCanceledException)
          {
            progressBar.Shout("Canceled!");
            return false;
          }
          catch (OperationCanceledException)
          {
            progressBar.Shout("Canceled!");
            return false;
          }
          progressBar.Shout("Saving");
          Tools.WriteBinaryFile(request.FileName, bytes);
          progressBar.Shout("Done!");
        }
        return true;
      }
    }
  }
}
