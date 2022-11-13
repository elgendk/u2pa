using U2Pa.Lib;
namespace U2pa.Electron.Link
{
  public class GuiProgressBar : IProgressBar
  {
    private GuiState state;
    private Timer? refreshTimer;

    public GuiProgressBar(GuiState state) => this.state = state; 

    public void Dispose()
    {
      refreshTimer?.Dispose();
    }

    public void Init(int size)
    {
      Dispose();
      state.MaxProgress = size;
      refreshTimer = new Timer(state.TimerCallBack, null, 0, 10);
    }

    public void Progress()
    {
      state.CurrentProgress++;
    }

    public void Shout(string message, params object[] obj)
    {
      state.CurrentProgressBarShout = String.Format(message, obj);
    }
  }
}
