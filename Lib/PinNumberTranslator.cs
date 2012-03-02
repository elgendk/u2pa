//                             u2pa
//
//    A command line interface for Top Universal Programmers
//
//    Copyright (C) Elgen };-) aka Morten Overgaard 2012
//
//    This file is part of u2pa.
//
//    u2pa is free software: you can redistribute it and/or modify
//    it under the terms of the GNU General Public License as published by
//    the Free Software Foundation, either version 3 of the License, or
//    (at your option) any later version.
//
//    Foobar is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//    GNU General Public License for more details.
//
//    You should have received a copy of the GNU General Public License
//    along with u2pa. If not, see <http://www.gnu.org/licenses/>.

namespace U2Pa.Lib
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

    public byte ToZIF(int dilPinNumber)
    {
      if (dilPinNumber < 0) dilPinNumber = -dilPinNumber;
      if (dilPinNumber <= dilIndex)
        return (byte)(dilPinNumber + 20 - dilIndex - placement);
      return (byte)(dilPinNumber + 20 - dilIndex + placement);
    }
  }
}