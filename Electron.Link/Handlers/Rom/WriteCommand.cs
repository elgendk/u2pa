using MediatR;
using U2Pa.Lib;
using U2Pa.Lib.IC;

namespace U2pa.Electron.Link.Handlers.Rom
{
  public class WriteCommand : IRequest<bool>
  {
    public GuiState State { get; set; }
    public string RomType { get; set; }
    public string FileName { get; set; }

    public WriteCommand(GuiState guiState, string romType, string fileName)
    {
      State = guiState;
      RomType = romType;
      FileName = fileName;
    }

    public class Handler : IRequestHandler<WriteCommand, bool>
    {
      public async Task<bool> Handle(WriteCommand request, CancellationToken cancellationToken)
      {
        var shouter = new GuiShouter(request.State);
        using (var progressBar = new GuiProgressBar(request.State))
        {
          progressBar.Shout("Reading file");
          var bytes = Tools.ReadBinaryFile(request.FileName).ToList();
          progressBar.Shout("Initializing ...");
          try
          {
            await Task.Run(() =>
            {
              var eprom = EpromXml.Specified[request.RomType];
              using (var topDevice = TopDevice.Create(shouter))
              {
                progressBar.Shout("Writing");
                topDevice.WriteEpromClassic(eprom, progressBar, bytes, cancellationToken);
              }
              progressBar.Shout("Done writing");
            }, cancellationToken);
          }
          catch (TaskCanceledException)
          {
            progressBar.Shout("Canceled!");
            return false;
          }
          catch (OperationCanceledException)
          {
            progressBar.Shout("Canceled!");
            return false;
          }
          progressBar.Shout("Done!");
        }
        return true;
      }
    }
  }
}
