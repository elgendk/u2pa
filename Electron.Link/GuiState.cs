namespace U2pa.Electron.Link
{
  public class GuiState
  {
    public int MaxProgress { get; internal set; }
    public int CurrentProgress { get; internal set; }
    public string CurrentProgressBarShout { get; internal set; } = null!;
    public int VerbosityLevel { get; set; }
    public Action<string> ShoutCallBack { get; internal set; }
    public TimerCallback TimerCallBack { get; private set; }

    public GuiState(
      TimerCallback? timerCallBack = null,
      Action<string>? shoutCallBack = null,
      int verbosityLevel = 3)
    {
      TimerCallBack = timerCallBack ?? (o => { });
      ShoutCallBack = shoutCallBack ?? (s => { });
      VerbosityLevel = verbosityLevel;
    }

    public void Reset()
    {
      MaxProgress = 100;
      CurrentProgress = 0;
      CurrentProgressBarShout = null!;
    }
  }
}
