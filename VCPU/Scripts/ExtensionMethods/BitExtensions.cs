
namespace VirtualCPU.ExtensionMethods
{
    public static class BitExtensions
    {
        #region Bit operations
        /// <summary>
        /// Retrieves the value of the bit at the specified index in the value.
        /// </summary>
        public static int GetBit(this int b, int index)
        {
            return (b >> index) & 0x01;
        }

        /// <summary>
        /// Sets the bit at the specified index to 1.
        /// </summary>
        public static int SetBit(this int b, int index)
        {
            return b | (0x01 << index);
        }

        /// <summary>
        /// Clears the bit at the specified index to 0.
        /// </summary>
        public static int ClearBit(this int b, int index)
        {
            return b & ~(0x01 << index);
        }
        #endregion

        #region Nibble (4 bits) operations

        /// <summary>
        /// Gets the value of the nibble (4 bits) at the specified index.
        /// </summary>
        public static int GetNibble(this int b, bool index)
        {
            return (b >> (index ? 4 : 0)) & 0x0F;
        }

        /// <summary>
        /// Sets the value of the nibble (4 bits) at the specified index.
        /// </summary>
        public static int SetNibble(this int b, int index, int value)
        {
            return (b & ~(0x0F << (index * 4))) | ((value & 0x0F) << (index * 4));
        }

        /// <summary>
        /// Clears the nibble (4 bits) at the specified index to 0.
        /// </summary>
        public static int ClearNibble(this int b, int index)
        {
            return b & ~(0x0F << (index * 4));
        }
        #endregion
    }
}
