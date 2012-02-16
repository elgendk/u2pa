using System;
namespace U2Pa
{
  public class U2PaException : Exception
  {
    public U2PaException(string message, params object[] obj) 
      : base(String.Format(message, obj))
    {
    }
  }
}