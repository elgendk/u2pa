using U2Pa.Lib;

namespace U2pa.Electron.Link
{
  public class GuiShouter : IShouter
  {
    private GuiState guiProgressState;

    public int VerbosityLevel 
    {
      get => guiProgressState.VerbosityLevel; 
      set => guiProgressState.VerbosityLevel = value; 
    }

    public GuiShouter(GuiState guiProgressState) => this.guiProgressState = guiProgressState;

    
    public void ShoutLine(int verbosity, string message, params object[] obj)
    {
      if (verbosity <= VerbosityLevel && guiProgressState.ShoutCallBack != null)
      {
        guiProgressState.ShoutCallBack(String.Format((VerbosityLevel == 5 ? "V" + verbosity + ": " : "") + message, obj));
      }
    }
  }
}
