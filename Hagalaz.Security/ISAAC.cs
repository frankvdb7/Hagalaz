using System;

namespace Hagalaz.Security
{
    /// <summary>
    /// Provides packet opcode encryption and decryption.
    /// </summary>
    public class ISAAC
    {
        /// <summary>
        /// Contains ISAAC keys.
        /// </summary>
        private readonly uint[] _keySet;
        /// <summary>
        /// Unknown value.
        /// </summary>
        private uint _keysetsGenerated;
        /// <summary>
        /// Contains current index in crypt array.
        /// </summary>
        private int _keyArrayIndex;
        /// <summary>
        /// Unknown value.
        /// </summary>
        private uint _accumulator;
        /// <summary>
        /// Unknown value.
        /// </summary>
        private uint _lastKey;
        /// <summary>
        /// Contains cryption array.
        /// </summary>
        private readonly uint[] _cryptSet;

        /// <summary>
        /// Contains golden radio.
        /// </summary>
        private const uint _goldenRatio = 0x9e3779b9;
        private static readonly uint _a = _goldenRatio;
        private static readonly uint _b = _goldenRatio;
        private static readonly uint _c = _goldenRatio;
        private static readonly uint _d = _goldenRatio;
        private static readonly uint _e = _goldenRatio;
        private static readonly uint _f = _goldenRatio;
        private static readonly uint _g = _goldenRatio;
        private static readonly uint _h = _goldenRatio;

        /// <summary>
        /// Construct's new ISAAC instance with given values.
        /// </summary>
        /// <param name="seed"></param>
        public ISAAC(uint[] seed)
        {
            if (seed is null)
            {
                throw new ArgumentNullException(nameof(seed));
            }

            _keySet = new uint[256];
            _cryptSet = new uint[256];
            for (var i = 0; seed.Length > i; i++)
            {
                _keySet[i] = seed[i];
            }
            InitKeySet();
        }

        /// <summary>
        /// Initializes cryption.
        /// </summary>
        private void InitKeySet()
        {
            var vA = _a;
            var vB = _b;
            var vC = _c;
            var vD = _d;
            var vE = _e;
            var vF = _f;
            var vG = _g;
            var vH = _h;
            for (var ac = 0; ac < 256; ac += 8)
            {
                vA += _keySet[ac + 1];
                vB += _keySet[ac + 2];
                vC += _keySet[ac + 3];
                vD += _keySet[ac + 4];
                vE += _keySet[ac + 5];
                vF += _keySet[ac + 6];
                vG += _keySet[ac + 7];
                vH += _keySet[ac];
                vH ^= vA << 11;
                vC += vH;
                vA += vB;
                vA ^= vB >> 2;
                vD += vA;
                vB += vC;
                vB ^= vC << 8;
                vC += vD;
                vE += vB;
                vC ^= vD >> 16;
                vD += vE;
                vF += vC;
                vD ^= vE << 10;
                vE += vF;
                vG += vD;
                vE ^= vF >> 4;
                vH += vE;
                vF += vG;
                vF ^= vG << 8;
                vA += vF;
                vG += vH;
                vG ^= vH >> 9;
                vH += vA;
                vB += vG;
                _cryptSet[ac] = vH;
                _cryptSet[ac + 1] = vA;
                _cryptSet[ac + 2] = vB;
                _cryptSet[ac + 3] = vC;
                _cryptSet[ac + 4] = vD;
                _cryptSet[ac + 5] = vE;
                _cryptSet[ac + 6] = vF;
                _cryptSet[ac + 7] = vG;
            }
            for (int ac = 0; ac < 256; ac += 8)
            {
                vD += _cryptSet[ac + 4];
                vH += _cryptSet[ac];
                vB += _cryptSet[ac + 2];
                vE += _cryptSet[ac + 5];
                vC += _cryptSet[ac + 3];
                vF += _cryptSet[ac + 6];
                vA += _cryptSet[ac + 1];
                vG += _cryptSet[ac + 7];
                vH ^= vA << 11;
                vA += vB;
                vC += vH;
                vA ^= vB >> 2;
                vD += vA;
                vB += vC;
                vB ^= vC << 8;
                vE += vB;
                vC += vD;
                vC ^= vD >> 16;
                vD += vE;
                vF += vC;
                vD ^= vE << 10;
                vE += vF;
                vG += vD;
                vE ^= vF >> 4;
                vF += vG;
                vH += vE;
                vF ^= vG << 8;
                vA += vF;
                vG += vH;
                vG ^= vH >> 9;
                vH += vA;
                vB += vG;
                _cryptSet[ac] = vH;
                _cryptSet[ac + 1] = vA;
                _cryptSet[ac + 2] = vB;
                _cryptSet[ac + 3] = vC;
                _cryptSet[ac + 4] = vD;
                _cryptSet[ac + 5] = vE;
                _cryptSet[ac + 6] = vF;
                _cryptSet[ac + 7] = vG;
            }
            NextKeySet();
            _keyArrayIndex = 256;
        }

        /// <summary>
        /// Generate's next key set.
        /// </summary>
        private void NextKeySet()
        {
            _lastKey += ++_keysetsGenerated;
            for (var i = 0; i < 256; i++)
            {
                var cryptKey = _cryptSet[i];
                if ((i & 0x2) != 0)
                {
                    if ((i & 0x1) == 0)
                        _accumulator ^= _accumulator << 2;
                    else
                        _accumulator ^= _accumulator >> 16;
                }
                else if ((i & 0x1) != 0)
                    _accumulator ^= _accumulator >> 6;
                else
                    _accumulator ^= _accumulator << 13;
                _accumulator += _cryptSet[i + 128 & 0xff];
                uint v;
                _cryptSet[i] = v = (_cryptSet[255 & (cryptKey >> 2)] + _accumulator + _lastKey);
                _keySet[i] = _lastKey = cryptKey + (_cryptSet[((v >> 8) & 1020) >> 2]);
            }
        }

        /// <summary>
        /// Read's key and goes to next.
        /// </summary>
        /// <returns></returns>
        public uint ReadKey()
        {
            Next();
            return _keySet[_keyArrayIndex];
        }

        /// <summary>
        /// Peek's key    
        /// </summary>
        /// <returns></returns>
        public uint PeekKey()
        {
            CheckNextKeySet();
            return _keySet[_keyArrayIndex - 1];
        }

        /// <summary>
        /// Advances to the next key set
        /// </summary>
        public void Next()
        {
            CheckNextKeySet();
            _keyArrayIndex--;
        }

        private void CheckNextKeySet()
        {
            if (_keyArrayIndex == 0)
            {
                NextKeySet();
                _keyArrayIndex = 256;
            }
        }

        static ISAAC()
        {
            for (var ac = 0; ac < 4; ac++)
            {
                _h ^= _a << 11;
                _c += _h;
                _a += _b;
                _a ^= _b >> 2;
                _b += _c;
                _d += _a;
                _b ^= _c << 8;
                _c += _d;
                _e += _b;
                _c ^= _d >> 16;
                _d += _e;
                _f += _c;
                _d ^= _e << 10;
                _e += _f;
                _g += _d;
                _e ^= _f >> 4;
                _h += _e;
                _f += _g;
                _f ^= _g << 8;
                _a += _f;
                _g += _h;
                _g ^= _h >> 9;
                _b += _g;
                _h += _a;
            }
        }
    }
}
