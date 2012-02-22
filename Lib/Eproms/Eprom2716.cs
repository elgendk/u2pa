namespace U2Pa.Lib.Eproms
{
  public class Eprom2716 : Eprom
  {
    public Eprom2716()
    {
      Type = "2716";
      DilType = 24;
      Placement = 0;
      AddressPins = new[] {8, 7, 6, 5, 4, 3, 2, 1, 23, 22, 19};
      DataPins = new[] {9, 10, 11, 13, 14, 15, 16, 17};
      EnablePins = new[] {18, 20};
      VccLevel = VccLevel.Vcc_5_0v;
      VppLevel = VppLevel.Vpp_Off;
      VccPins = new byte[] {32};
      GndPins = new byte[] {20};
    }
  }
}