namespace U2Pa.Eproms
{
  public abstract class Eprom
  {
    public string Type;
    public int DilType;
    public int[] AddressPins;
    public int[] DataPins;
    public int[] EnablePins;
    public VccLevel VccLevel;
    public VppLevel VppLevel;
    public byte[] VccPins;
    public byte[] GndPins;
    public byte VppPin;

    public static Eprom Create(string type)
    {
      if (type == "2716")
        return new Eprom2716();
      if (type == "272048")
        return new Eprom272048();

      throw new U2PaException("Unsupported EPROM: {0}", type);
    }
  }
}