// Copied from the MaxLoader Manual:
// =================================
// The following are valid characters for test vectors:
// 0 Apply input logic low (Vil) to an input pin
// 1 Apply input logic high (Vih) to an input pin
// C Clock an input pin (Vil, Vih, Vil)
// F Float pin
// N Power pin or untested output pin
// V VCC pin
// X Don't care: output values are not tested
// G GND pin
// K Clock an inverted input pin (Vih, Vil, Vih)
// H Expected result on output pin is Vih
// L Expected result on output pin is Vil
// Z Test for high impedance

// Start with:
// ===========
// 0 Apply input logic low (Vil) to an input pin
// 1 Apply input logic high (Vih) to an input pin
// V VCC pin
// X Don't care: output values are not tested
// G GND pin
// H Expected result on output pin is Vih
// L Expected result on output pin is Vil

public enum VectorValues
{
  /// <summary>
  /// 0 Apply input logic low (Vil) to an input pin.
  /// </summary>
  Zero,

  /// <summary>
  /// 1 Apply input logic high (Vih) to an input pin.
  /// </summary>
  One,
  
  /// <summary>
  /// V VCC pin.
  /// </summary>
  Vcc,

  /// <summary>
  /// X Don't care: output values are not tested.
  /// </summary>
  DontCare,
  
  /// <summary>
  /// G GND pin.
  /// </summary>
  Gnd,

  /// <summary>
  /// H Expected result on output pin is Vih.
  /// </summary>
  High,

  /// <summary>
  /// L Expected result on output pin is Vil.
  /// </summary>
  Low
}
