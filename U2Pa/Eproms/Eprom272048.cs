namespace U2Pa.Eproms
{
  public class Eprom272048 : Eprom
  {
    public Eprom272048()
    {
      Type = "272048";
      DilType = 40;
      AddressPins = new[] {21, 22, 23, 24, 25, 26, 27, 28, 29, 31, 32, 33, 34, 35, 36, 37, 38};
      DataPins = new[] {19, 18, 17, 16, 15, 14, 13, 12, 10, 9, 8, 7, 6, 5, 4, 3};
      EnablePins = new[] {2};
      VccLevel = VccLevel.Vcc_5_0v;
      VppLevel = VppLevel.Vpp_Off;
      VccPins = new byte[] {40};
      GndPins = new byte[] {20};
    }
  }
}