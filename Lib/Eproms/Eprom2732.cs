namespace U2Pa.Lib.Eproms
{
  public class Eprom2732 : Eprom
  {
    public Eprom2732()
    {
      Type = "2732";
      DilType = 24;
      Placement = 0;
      AddressPins = new[] {8, 7, 6, 5, 4, 3, 2, 1, 23, 22, 19, 21};
      DataPins = new[] {9, 10, 11, 13, 14, 15, 16, 17};
      EnablePins = new[] {18, 20};
      VccLevel = VccLevel.Vcc_5_0v;
      VppLevel = VppLevel.Vpp_Off;
      VppPins = new byte[] {20};
      VccPins = new byte[] {32};
      GndPins = new byte[] {12};
    }
  }
}