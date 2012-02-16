namespace U2Pa
{
  public class PinNumberTranslator
  {
    private readonly int dilIndex;
    private readonly int dilType;
    private readonly int placement;

    public PinNumberTranslator(int dilType, int placement)
    {
      if (dilType % 2 != 0)
        throw new U2PaException("dilType must be even, but was {0}", dilType);
      this.dilType = dilType;
      dilIndex = dilType / 2;
      if (placement + dilIndex > 20)
        throw new U2PaException("DIL{0} can't be placed at position {1}", dilType, placement);
      this.placement = placement;
    }

    public int ToDIL(int zifPinNumer)
    {
      int returnValue;
      if (zifPinNumer <= 20)
      {
        returnValue = zifPinNumer - 20 + dilIndex + placement;
        return returnValue > dilIndex || returnValue < 0 ? 0 : returnValue;
      }
      
      returnValue = zifPinNumer - 20 + dilIndex - placement;
      return returnValue <= dilIndex || returnValue > dilType ? 0 : returnValue;
    }

    public int ToZIF(int dilPinNumber)
    {
      if (dilPinNumber <= dilIndex)
        return dilPinNumber + 20 - dilIndex - placement;
      return dilPinNumber + 20 - dilIndex + placement;
    }
  }
}