/* Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *   http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

/* This software is Public Domain software and may be freely copied,
 * modified, or distributed.
 * 
 * THIS SOFTWARE IS PROVIDED BY THE AUTHORS 'AS IS' AND ANY EXPRESS
 * OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED.  IN NO EVENT SHALL THE AUTHORS OR CONTRIBUTORS BE
 * LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
 * CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
 * SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR
 * BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY,
 * WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE
 * OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE,
 * EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 * 
 * ---------------------------------------------------------------------
 * 
 * For countries that do not provide for Public Domain this class is 
 * dual licensed under the Apache License at the top of this file.
 */

using System;

namespace Hagalaz.Cache.Utilities
{
    /// <summary>
    /// The Whirlpool hashing function.
    /// References
    /// The Whirlpool algorithm was developed by Paulo S. L. M. Barreto and Vincent Rijmen.
    /// See
    /// P.S.L.M. Barreto, V. Rijmen,
    /// ``The Whirlpool hashing function,''
    /// NESSIE submission, 2000 (tweaked version, 2001),
    /// https://www.cosic.esat.kuleuven.ac.be/nessie/workshop/submissions/whirlpool.zip
    /// @author  Paulo S.L.M. Barreto
    /// @author  Vincent Rijmen.
    /// =============================================================================
    /// .Net Coversion on May 2010 by Roger O Knapp http://csharptest.net
    /// Performance considerations: coult be improved by moving to unmanaged/unsafe
    /// implementations, currently SHA512 is about 10% faster.
    /// The code remains, as much as was possible, exactly as it appears in the
    /// example 'c' implementation available from the documentation package on
    /// http://www.larc.usp.br/~pbarreto/WhirlpoolPage.html
    /// =============================================================================
    /// @version 3.0 (2003.03.12)
    /// =============================================================================
    /// Differences from version 2.1:
    /// - Suboptimal diffusion matrix replaced by cir(1, 1, 4, 1, 8, 5, 2, 9).
    /// =============================================================================
    /// Differences from version 2.0:
    /// - Generation of ISO/IEC 10118-3 test vectors.
    /// - Bug fix: nonzero carry was ignored when tallying the data count
    /// (this bug apparently only manifested itself when feeding data
    /// in pieces rather than in a single chunk at once).
    /// - Support for MS Visual C++ 64-bit integer arithmetic.
    /// Differences from version 1.0:
    /// - Original S-box replaced by the tweaked, hardware-efficient version.
    /// =============================================================================
    /// THIS SOFTWARE IS PROVIDED BY THE AUTHORS ''AS IS'' AND ANY EXPRESS
    /// OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
    /// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
    /// ARE DISCLAIMED.  IN NO EVENT SHALL THE AUTHORS OR CONTRIBUTORS BE
    /// LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
    /// CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
    /// SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR
    /// BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY,
    /// WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE
    /// OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE,
    /// EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
    /// </summary>
    public class Whirlpool : System.Security.Cryptography.HashAlgorithm
    {
        /// <summary>
        /// Gets the digest.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="length">The length.</param>
        /// <returns>System.Byte[][].</returns>
        public static byte[] GenerateDigest(byte[] data, int offset, int length)
        {
            byte[] source;
            if (offset <= 0) 
            {
                source = data;
            } 
            else 
            {
                source = new byte[length];
                for (int i = 0; i < length; i++)
                {
                    source[i] = data[offset + i];
                }
            }
            byte[] digest = new byte[64];
            using (Whirlpool whirlpool = new Whirlpool())
            {
                whirlpool.Initialize();
                whirlpool.NessiEadd(source, 0, length); // Doesn't need * 8, as this gets done in NESSIEadd.
                whirlpool.NessiEfinalize(digest);
            }
            return digest;
        }

        /// <summary>
        /// Re-initializes the hash algorithm data structures.
        /// </summary>
        public override void Initialize() => NessiEinit();

        /// <summary>
        /// Adds the provided bytes to the hash
        /// </summary>
        /// <param name="array">The input to compute the hash code for.</param>
        /// <param name="ibStart">The offset into the byte array from which to begin using data.</param>
        /// <param name="cbSize">The number of bytes in the byte array to use as data.</param>
        protected override void HashCore(byte[] array, int ibStart, int cbSize) => NessiEadd(array, ibStart, cbSize);

        /// <summary>
        /// Returns the resulting hash code
        /// </summary>
        /// <returns>The computed hash code.</returns>
        protected override byte[] HashFinal()
        {
            byte[] result = new byte[Digestbytes];
            NessiEfinalize(result);
            return result;
        }

        /// <summary>
        /// LLs the specified ul.
        /// </summary>
        /// <param name="ul">The ul.</param>
        /// <returns>u64.</returns>
        private static ulong Ll(ulong ul) => ul;

        /// <summary>
        /// 
        /// </summary>
        static readonly ulong[] C0 = new ulong[256] {
            Ll(0x18186018c07830d8), Ll(0x23238c2305af4626), Ll(0xc6c63fc67ef991b8), Ll(0xe8e887e8136fcdfb),
            Ll(0x878726874ca113cb), Ll(0xb8b8dab8a9626d11), Ll(0x0101040108050209), Ll(0x4f4f214f426e9e0d),
            Ll(0x3636d836adee6c9b), Ll(0xa6a6a2a6590451ff), Ll(0xd2d26fd2debdb90c), Ll(0xf5f5f3f5fb06f70e),
            Ll(0x7979f979ef80f296), Ll(0x6f6fa16f5fcede30), Ll(0x91917e91fcef3f6d), Ll(0x52525552aa07a4f8),
            Ll(0x60609d6027fdc047), Ll(0xbcbccabc89766535), Ll(0x9b9b569baccd2b37), Ll(0x8e8e028e048c018a),
            Ll(0xa3a3b6a371155bd2), Ll(0x0c0c300c603c186c), Ll(0x7b7bf17bff8af684), Ll(0x3535d435b5e16a80),
            Ll(0x1d1d741de8693af5), Ll(0xe0e0a7e05347ddb3), Ll(0xd7d77bd7f6acb321), Ll(0xc2c22fc25eed999c),
            Ll(0x2e2eb82e6d965c43), Ll(0x4b4b314b627a9629), Ll(0xfefedffea321e15d), Ll(0x575741578216aed5),
            Ll(0x15155415a8412abd), Ll(0x7777c1779fb6eee8), Ll(0x3737dc37a5eb6e92), Ll(0xe5e5b3e57b56d79e),
            Ll(0x9f9f469f8cd92313), Ll(0xf0f0e7f0d317fd23), Ll(0x4a4a354a6a7f9420), Ll(0xdada4fda9e95a944),
            Ll(0x58587d58fa25b0a2), Ll(0xc9c903c906ca8fcf), Ll(0x2929a429558d527c), Ll(0x0a0a280a5022145a),
            Ll(0xb1b1feb1e14f7f50), Ll(0xa0a0baa0691a5dc9), Ll(0x6b6bb16b7fdad614), Ll(0x85852e855cab17d9),
            Ll(0xbdbdcebd8173673c), Ll(0x5d5d695dd234ba8f), Ll(0x1010401080502090), Ll(0xf4f4f7f4f303f507),
            Ll(0xcbcb0bcb16c08bdd), Ll(0x3e3ef83eedc67cd3), Ll(0x0505140528110a2d), Ll(0x676781671fe6ce78),
            Ll(0xe4e4b7e47353d597), Ll(0x27279c2725bb4e02), Ll(0x4141194132588273), Ll(0x8b8b168b2c9d0ba7),
            Ll(0xa7a7a6a7510153f6), Ll(0x7d7de97dcf94fab2), Ll(0x95956e95dcfb3749), Ll(0xd8d847d88e9fad56),
            Ll(0xfbfbcbfb8b30eb70), Ll(0xeeee9fee2371c1cd), Ll(0x7c7ced7cc791f8bb), Ll(0x6666856617e3cc71),
            Ll(0xdddd53dda68ea77b), Ll(0x17175c17b84b2eaf), Ll(0x4747014702468e45), Ll(0x9e9e429e84dc211a),
            Ll(0xcaca0fca1ec589d4), Ll(0x2d2db42d75995a58), Ll(0xbfbfc6bf9179632e), Ll(0x07071c07381b0e3f),
            Ll(0xadad8ead012347ac), Ll(0x5a5a755aea2fb4b0), Ll(0x838336836cb51bef), Ll(0x3333cc3385ff66b6),
            Ll(0x636391633ff2c65c), Ll(0x02020802100a0412), Ll(0xaaaa92aa39384993), Ll(0x7171d971afa8e2de),
            Ll(0xc8c807c80ecf8dc6), Ll(0x19196419c87d32d1), Ll(0x494939497270923b), Ll(0xd9d943d9869aaf5f),
            Ll(0xf2f2eff2c31df931), Ll(0xe3e3abe34b48dba8), Ll(0x5b5b715be22ab6b9), Ll(0x88881a8834920dbc),
            Ll(0x9a9a529aa4c8293e), Ll(0x262698262dbe4c0b), Ll(0x3232c8328dfa64bf), Ll(0xb0b0fab0e94a7d59),
            Ll(0xe9e983e91b6acff2), Ll(0x0f0f3c0f78331e77), Ll(0xd5d573d5e6a6b733), Ll(0x80803a8074ba1df4),
            Ll(0xbebec2be997c6127), Ll(0xcdcd13cd26de87eb), Ll(0x3434d034bde46889), Ll(0x48483d487a759032),
            Ll(0xffffdbffab24e354), Ll(0x7a7af57af78ff48d), Ll(0x90907a90f4ea3d64), Ll(0x5f5f615fc23ebe9d),
            Ll(0x202080201da0403d), Ll(0x6868bd6867d5d00f), Ll(0x1a1a681ad07234ca), Ll(0xaeae82ae192c41b7),
            Ll(0xb4b4eab4c95e757d), Ll(0x54544d549a19a8ce), Ll(0x93937693ece53b7f), Ll(0x222288220daa442f),
            Ll(0x64648d6407e9c863), Ll(0xf1f1e3f1db12ff2a), Ll(0x7373d173bfa2e6cc), Ll(0x12124812905a2482),
            Ll(0x40401d403a5d807a), Ll(0x0808200840281048), Ll(0xc3c32bc356e89b95), Ll(0xecec97ec337bc5df),
            Ll(0xdbdb4bdb9690ab4d), Ll(0xa1a1bea1611f5fc0), Ll(0x8d8d0e8d1c830791), Ll(0x3d3df43df5c97ac8),
            Ll(0x97976697ccf1335b), Ll(0x0000000000000000), Ll(0xcfcf1bcf36d483f9), Ll(0x2b2bac2b4587566e),
            Ll(0x7676c57697b3ece1), Ll(0x8282328264b019e6), Ll(0xd6d67fd6fea9b128), Ll(0x1b1b6c1bd87736c3),
            Ll(0xb5b5eeb5c15b7774), Ll(0xafaf86af112943be), Ll(0x6a6ab56a77dfd41d), Ll(0x50505d50ba0da0ea),
            Ll(0x45450945124c8a57), Ll(0xf3f3ebf3cb18fb38), Ll(0x3030c0309df060ad), Ll(0xefef9bef2b74c3c4),
            Ll(0x3f3ffc3fe5c37eda), Ll(0x55554955921caac7), Ll(0xa2a2b2a2791059db), Ll(0xeaea8fea0365c9e9),
            Ll(0x656589650fecca6a), Ll(0xbabad2bab9686903), Ll(0x2f2fbc2f65935e4a), Ll(0xc0c027c04ee79d8e),
            Ll(0xdede5fdebe81a160), Ll(0x1c1c701ce06c38fc), Ll(0xfdfdd3fdbb2ee746), Ll(0x4d4d294d52649a1f),
            Ll(0x92927292e4e03976), Ll(0x7575c9758fbceafa), Ll(0x06061806301e0c36), Ll(0x8a8a128a249809ae),
            Ll(0xb2b2f2b2f940794b), Ll(0xe6e6bfe66359d185), Ll(0x0e0e380e70361c7e), Ll(0x1f1f7c1ff8633ee7),
            Ll(0x6262956237f7c455), Ll(0xd4d477d4eea3b53a), Ll(0xa8a89aa829324d81), Ll(0x96966296c4f43152),
            Ll(0xf9f9c3f99b3aef62), Ll(0xc5c533c566f697a3), Ll(0x2525942535b14a10), Ll(0x59597959f220b2ab),
            Ll(0x84842a8454ae15d0), Ll(0x7272d572b7a7e4c5), Ll(0x3939e439d5dd72ec), Ll(0x4c4c2d4c5a619816),
            Ll(0x5e5e655eca3bbc94), Ll(0x7878fd78e785f09f), Ll(0x3838e038ddd870e5), Ll(0x8c8c0a8c14860598),
            Ll(0xd1d163d1c6b2bf17), Ll(0xa5a5aea5410b57e4), Ll(0xe2e2afe2434dd9a1), Ll(0x616199612ff8c24e),
            Ll(0xb3b3f6b3f1457b42), Ll(0x2121842115a54234), Ll(0x9c9c4a9c94d62508), Ll(0x1e1e781ef0663cee),
            Ll(0x4343114322528661), Ll(0xc7c73bc776fc93b1), Ll(0xfcfcd7fcb32be54f), Ll(0x0404100420140824),
            Ll(0x51515951b208a2e3), Ll(0x99995e99bcc72f25), Ll(0x6d6da96d4fc4da22), Ll(0x0d0d340d68391a65),
            Ll(0xfafacffa8335e979), Ll(0xdfdf5bdfb684a369), Ll(0x7e7ee57ed79bfca9), Ll(0x242490243db44819),
            Ll(0x3b3bec3bc5d776fe), Ll(0xabab96ab313d4b9a), Ll(0xcece1fce3ed181f0), Ll(0x1111441188552299),
            Ll(0x8f8f068f0c890383), Ll(0x4e4e254e4a6b9c04), Ll(0xb7b7e6b7d1517366), Ll(0xebeb8beb0b60cbe0),
            Ll(0x3c3cf03cfdcc78c1), Ll(0x81813e817cbf1ffd), Ll(0x94946a94d4fe3540), Ll(0xf7f7fbf7eb0cf31c),
            Ll(0xb9b9deb9a1676f18), Ll(0x13134c13985f268b), Ll(0x2c2cb02c7d9c5851), Ll(0xd3d36bd3d6b8bb05),
            Ll(0xe7e7bbe76b5cd38c), Ll(0x6e6ea56e57cbdc39), Ll(0xc4c437c46ef395aa), Ll(0x03030c03180f061b),
            Ll(0x565645568a13acdc), Ll(0x44440d441a49885e), Ll(0x7f7fe17fdf9efea0), Ll(0xa9a99ea921374f88),
            Ll(0x2a2aa82a4d825467), Ll(0xbbbbd6bbb16d6b0a), Ll(0xc1c123c146e29f87), Ll(0x53535153a202a6f1),
            Ll(0xdcdc57dcae8ba572), Ll(0x0b0b2c0b58271653), Ll(0x9d9d4e9d9cd32701), Ll(0x6c6cad6c47c1d82b),
            Ll(0x3131c43195f562a4), Ll(0x7474cd7487b9e8f3), Ll(0xf6f6fff6e309f115), Ll(0x464605460a438c4c),
            Ll(0xacac8aac092645a5), Ll(0x89891e893c970fb5), Ll(0x14145014a04428b4), Ll(0xe1e1a3e15b42dfba),
            Ll(0x16165816b04e2ca6), Ll(0x3a3ae83acdd274f7), Ll(0x6969b9696fd0d206), Ll(0x09092409482d1241),
            Ll(0x7070dd70a7ade0d7), Ll(0xb6b6e2b6d954716f), Ll(0xd0d067d0ceb7bd1e), Ll(0xeded93ed3b7ec7d6),
            Ll(0xcccc17cc2edb85e2), Ll(0x424215422a578468), Ll(0x98985a98b4c22d2c), Ll(0xa4a4aaa4490e55ed),
            Ll(0x2828a0285d885075), Ll(0x5c5c6d5cda31b886), Ll(0xf8f8c7f8933fed6b), Ll(0x8686228644a411c2),
        };

        /// <summary>
        /// 
        /// </summary>
        static readonly ulong[] C1 = new ulong[256] {
            Ll(0xd818186018c07830), Ll(0x2623238c2305af46), Ll(0xb8c6c63fc67ef991), Ll(0xfbe8e887e8136fcd),
            Ll(0xcb878726874ca113), Ll(0x11b8b8dab8a9626d), Ll(0x0901010401080502), Ll(0x0d4f4f214f426e9e),
            Ll(0x9b3636d836adee6c), Ll(0xffa6a6a2a6590451), Ll(0x0cd2d26fd2debdb9), Ll(0x0ef5f5f3f5fb06f7),
            Ll(0x967979f979ef80f2), Ll(0x306f6fa16f5fcede), Ll(0x6d91917e91fcef3f), Ll(0xf852525552aa07a4),
            Ll(0x4760609d6027fdc0), Ll(0x35bcbccabc897665), Ll(0x379b9b569baccd2b), Ll(0x8a8e8e028e048c01),
            Ll(0xd2a3a3b6a371155b), Ll(0x6c0c0c300c603c18), Ll(0x847b7bf17bff8af6), Ll(0x803535d435b5e16a),
            Ll(0xf51d1d741de8693a), Ll(0xb3e0e0a7e05347dd), Ll(0x21d7d77bd7f6acb3), Ll(0x9cc2c22fc25eed99),
            Ll(0x432e2eb82e6d965c), Ll(0x294b4b314b627a96), Ll(0x5dfefedffea321e1), Ll(0xd5575741578216ae),
            Ll(0xbd15155415a8412a), Ll(0xe87777c1779fb6ee), Ll(0x923737dc37a5eb6e), Ll(0x9ee5e5b3e57b56d7),
            Ll(0x139f9f469f8cd923), Ll(0x23f0f0e7f0d317fd), Ll(0x204a4a354a6a7f94), Ll(0x44dada4fda9e95a9),
            Ll(0xa258587d58fa25b0), Ll(0xcfc9c903c906ca8f), Ll(0x7c2929a429558d52), Ll(0x5a0a0a280a502214),
            Ll(0x50b1b1feb1e14f7f), Ll(0xc9a0a0baa0691a5d), Ll(0x146b6bb16b7fdad6), Ll(0xd985852e855cab17),
            Ll(0x3cbdbdcebd817367), Ll(0x8f5d5d695dd234ba), Ll(0x9010104010805020), Ll(0x07f4f4f7f4f303f5),
            Ll(0xddcbcb0bcb16c08b), Ll(0xd33e3ef83eedc67c), Ll(0x2d0505140528110a), Ll(0x78676781671fe6ce),
            Ll(0x97e4e4b7e47353d5), Ll(0x0227279c2725bb4e), Ll(0x7341411941325882), Ll(0xa78b8b168b2c9d0b),
            Ll(0xf6a7a7a6a7510153), Ll(0xb27d7de97dcf94fa), Ll(0x4995956e95dcfb37), Ll(0x56d8d847d88e9fad),
            Ll(0x70fbfbcbfb8b30eb), Ll(0xcdeeee9fee2371c1), Ll(0xbb7c7ced7cc791f8), Ll(0x716666856617e3cc),
            Ll(0x7bdddd53dda68ea7), Ll(0xaf17175c17b84b2e), Ll(0x454747014702468e), Ll(0x1a9e9e429e84dc21),
            Ll(0xd4caca0fca1ec589), Ll(0x582d2db42d75995a), Ll(0x2ebfbfc6bf917963), Ll(0x3f07071c07381b0e),
            Ll(0xacadad8ead012347), Ll(0xb05a5a755aea2fb4), Ll(0xef838336836cb51b), Ll(0xb63333cc3385ff66),
            Ll(0x5c636391633ff2c6), Ll(0x1202020802100a04), Ll(0x93aaaa92aa393849), Ll(0xde7171d971afa8e2),
            Ll(0xc6c8c807c80ecf8d), Ll(0xd119196419c87d32), Ll(0x3b49493949727092), Ll(0x5fd9d943d9869aaf),
            Ll(0x31f2f2eff2c31df9), Ll(0xa8e3e3abe34b48db), Ll(0xb95b5b715be22ab6), Ll(0xbc88881a8834920d),
            Ll(0x3e9a9a529aa4c829), Ll(0x0b262698262dbe4c), Ll(0xbf3232c8328dfa64), Ll(0x59b0b0fab0e94a7d),
            Ll(0xf2e9e983e91b6acf), Ll(0x770f0f3c0f78331e), Ll(0x33d5d573d5e6a6b7), Ll(0xf480803a8074ba1d),
            Ll(0x27bebec2be997c61), Ll(0xebcdcd13cd26de87), Ll(0x893434d034bde468), Ll(0x3248483d487a7590),
            Ll(0x54ffffdbffab24e3), Ll(0x8d7a7af57af78ff4), Ll(0x6490907a90f4ea3d), Ll(0x9d5f5f615fc23ebe),
            Ll(0x3d202080201da040), Ll(0x0f6868bd6867d5d0), Ll(0xca1a1a681ad07234), Ll(0xb7aeae82ae192c41),
            Ll(0x7db4b4eab4c95e75), Ll(0xce54544d549a19a8), Ll(0x7f93937693ece53b), Ll(0x2f222288220daa44),
            Ll(0x6364648d6407e9c8), Ll(0x2af1f1e3f1db12ff), Ll(0xcc7373d173bfa2e6), Ll(0x8212124812905a24),
            Ll(0x7a40401d403a5d80), Ll(0x4808082008402810), Ll(0x95c3c32bc356e89b), Ll(0xdfecec97ec337bc5),
            Ll(0x4ddbdb4bdb9690ab), Ll(0xc0a1a1bea1611f5f), Ll(0x918d8d0e8d1c8307), Ll(0xc83d3df43df5c97a),
            Ll(0x5b97976697ccf133), Ll(0x0000000000000000), Ll(0xf9cfcf1bcf36d483), Ll(0x6e2b2bac2b458756),
            Ll(0xe17676c57697b3ec), Ll(0xe68282328264b019), Ll(0x28d6d67fd6fea9b1), Ll(0xc31b1b6c1bd87736),
            Ll(0x74b5b5eeb5c15b77), Ll(0xbeafaf86af112943), Ll(0x1d6a6ab56a77dfd4), Ll(0xea50505d50ba0da0),
            Ll(0x5745450945124c8a), Ll(0x38f3f3ebf3cb18fb), Ll(0xad3030c0309df060), Ll(0xc4efef9bef2b74c3),
            Ll(0xda3f3ffc3fe5c37e), Ll(0xc755554955921caa), Ll(0xdba2a2b2a2791059), Ll(0xe9eaea8fea0365c9),
            Ll(0x6a656589650fecca), Ll(0x03babad2bab96869), Ll(0x4a2f2fbc2f65935e), Ll(0x8ec0c027c04ee79d),
            Ll(0x60dede5fdebe81a1), Ll(0xfc1c1c701ce06c38), Ll(0x46fdfdd3fdbb2ee7), Ll(0x1f4d4d294d52649a),
            Ll(0x7692927292e4e039), Ll(0xfa7575c9758fbcea), Ll(0x3606061806301e0c), Ll(0xae8a8a128a249809),
            Ll(0x4bb2b2f2b2f94079), Ll(0x85e6e6bfe66359d1), Ll(0x7e0e0e380e70361c), Ll(0xe71f1f7c1ff8633e),
            Ll(0x556262956237f7c4), Ll(0x3ad4d477d4eea3b5), Ll(0x81a8a89aa829324d), Ll(0x5296966296c4f431),
            Ll(0x62f9f9c3f99b3aef), Ll(0xa3c5c533c566f697), Ll(0x102525942535b14a), Ll(0xab59597959f220b2),
            Ll(0xd084842a8454ae15), Ll(0xc57272d572b7a7e4), Ll(0xec3939e439d5dd72), Ll(0x164c4c2d4c5a6198),
            Ll(0x945e5e655eca3bbc), Ll(0x9f7878fd78e785f0), Ll(0xe53838e038ddd870), Ll(0x988c8c0a8c148605),
            Ll(0x17d1d163d1c6b2bf), Ll(0xe4a5a5aea5410b57), Ll(0xa1e2e2afe2434dd9), Ll(0x4e616199612ff8c2),
            Ll(0x42b3b3f6b3f1457b), Ll(0x342121842115a542), Ll(0x089c9c4a9c94d625), Ll(0xee1e1e781ef0663c),
            Ll(0x6143431143225286), Ll(0xb1c7c73bc776fc93), Ll(0x4ffcfcd7fcb32be5), Ll(0x2404041004201408),
            Ll(0xe351515951b208a2), Ll(0x2599995e99bcc72f), Ll(0x226d6da96d4fc4da), Ll(0x650d0d340d68391a),
            Ll(0x79fafacffa8335e9), Ll(0x69dfdf5bdfb684a3), Ll(0xa97e7ee57ed79bfc), Ll(0x19242490243db448),
            Ll(0xfe3b3bec3bc5d776), Ll(0x9aabab96ab313d4b), Ll(0xf0cece1fce3ed181), Ll(0x9911114411885522),
            Ll(0x838f8f068f0c8903), Ll(0x044e4e254e4a6b9c), Ll(0x66b7b7e6b7d15173), Ll(0xe0ebeb8beb0b60cb),
            Ll(0xc13c3cf03cfdcc78), Ll(0xfd81813e817cbf1f), Ll(0x4094946a94d4fe35), Ll(0x1cf7f7fbf7eb0cf3),
            Ll(0x18b9b9deb9a1676f), Ll(0x8b13134c13985f26), Ll(0x512c2cb02c7d9c58), Ll(0x05d3d36bd3d6b8bb),
            Ll(0x8ce7e7bbe76b5cd3), Ll(0x396e6ea56e57cbdc), Ll(0xaac4c437c46ef395), Ll(0x1b03030c03180f06),
            Ll(0xdc565645568a13ac), Ll(0x5e44440d441a4988), Ll(0xa07f7fe17fdf9efe), Ll(0x88a9a99ea921374f),
            Ll(0x672a2aa82a4d8254), Ll(0x0abbbbd6bbb16d6b), Ll(0x87c1c123c146e29f), Ll(0xf153535153a202a6),
            Ll(0x72dcdc57dcae8ba5), Ll(0x530b0b2c0b582716), Ll(0x019d9d4e9d9cd327), Ll(0x2b6c6cad6c47c1d8),
            Ll(0xa43131c43195f562), Ll(0xf37474cd7487b9e8), Ll(0x15f6f6fff6e309f1), Ll(0x4c464605460a438c),
            Ll(0xa5acac8aac092645), Ll(0xb589891e893c970f), Ll(0xb414145014a04428), Ll(0xbae1e1a3e15b42df),
            Ll(0xa616165816b04e2c), Ll(0xf73a3ae83acdd274), Ll(0x066969b9696fd0d2), Ll(0x4109092409482d12),
            Ll(0xd77070dd70a7ade0), Ll(0x6fb6b6e2b6d95471), Ll(0x1ed0d067d0ceb7bd), Ll(0xd6eded93ed3b7ec7),
            Ll(0xe2cccc17cc2edb85), Ll(0x68424215422a5784), Ll(0x2c98985a98b4c22d), Ll(0xeda4a4aaa4490e55),
            Ll(0x752828a0285d8850), Ll(0x865c5c6d5cda31b8), Ll(0x6bf8f8c7f8933fed), Ll(0xc28686228644a411),
        };

        /// <summary>
        /// 
        /// </summary>
        static readonly ulong[] C2 = new ulong[256] {
            Ll(0x30d818186018c078), Ll(0x462623238c2305af), Ll(0x91b8c6c63fc67ef9), Ll(0xcdfbe8e887e8136f),
            Ll(0x13cb878726874ca1), Ll(0x6d11b8b8dab8a962), Ll(0x0209010104010805), Ll(0x9e0d4f4f214f426e),
            Ll(0x6c9b3636d836adee), Ll(0x51ffa6a6a2a65904), Ll(0xb90cd2d26fd2debd), Ll(0xf70ef5f5f3f5fb06),
            Ll(0xf2967979f979ef80), Ll(0xde306f6fa16f5fce), Ll(0x3f6d91917e91fcef), Ll(0xa4f852525552aa07),
            Ll(0xc04760609d6027fd), Ll(0x6535bcbccabc8976), Ll(0x2b379b9b569baccd), Ll(0x018a8e8e028e048c),
            Ll(0x5bd2a3a3b6a37115), Ll(0x186c0c0c300c603c), Ll(0xf6847b7bf17bff8a), Ll(0x6a803535d435b5e1),
            Ll(0x3af51d1d741de869), Ll(0xddb3e0e0a7e05347), Ll(0xb321d7d77bd7f6ac), Ll(0x999cc2c22fc25eed),
            Ll(0x5c432e2eb82e6d96), Ll(0x96294b4b314b627a), Ll(0xe15dfefedffea321), Ll(0xaed5575741578216),
            Ll(0x2abd15155415a841), Ll(0xeee87777c1779fb6), Ll(0x6e923737dc37a5eb), Ll(0xd79ee5e5b3e57b56),
            Ll(0x23139f9f469f8cd9), Ll(0xfd23f0f0e7f0d317), Ll(0x94204a4a354a6a7f), Ll(0xa944dada4fda9e95),
            Ll(0xb0a258587d58fa25), Ll(0x8fcfc9c903c906ca), Ll(0x527c2929a429558d), Ll(0x145a0a0a280a5022),
            Ll(0x7f50b1b1feb1e14f), Ll(0x5dc9a0a0baa0691a), Ll(0xd6146b6bb16b7fda), Ll(0x17d985852e855cab),
            Ll(0x673cbdbdcebd8173), Ll(0xba8f5d5d695dd234), Ll(0x2090101040108050), Ll(0xf507f4f4f7f4f303),
            Ll(0x8bddcbcb0bcb16c0), Ll(0x7cd33e3ef83eedc6), Ll(0x0a2d050514052811), Ll(0xce78676781671fe6),
            Ll(0xd597e4e4b7e47353), Ll(0x4e0227279c2725bb), Ll(0x8273414119413258), Ll(0x0ba78b8b168b2c9d),
            Ll(0x53f6a7a7a6a75101), Ll(0xfab27d7de97dcf94), Ll(0x374995956e95dcfb), Ll(0xad56d8d847d88e9f),
            Ll(0xeb70fbfbcbfb8b30), Ll(0xc1cdeeee9fee2371), Ll(0xf8bb7c7ced7cc791), Ll(0xcc716666856617e3),
            Ll(0xa77bdddd53dda68e), Ll(0x2eaf17175c17b84b), Ll(0x8e45474701470246), Ll(0x211a9e9e429e84dc),
            Ll(0x89d4caca0fca1ec5), Ll(0x5a582d2db42d7599), Ll(0x632ebfbfc6bf9179), Ll(0x0e3f07071c07381b),
            Ll(0x47acadad8ead0123), Ll(0xb4b05a5a755aea2f), Ll(0x1bef838336836cb5), Ll(0x66b63333cc3385ff),
            Ll(0xc65c636391633ff2), Ll(0x041202020802100a), Ll(0x4993aaaa92aa3938), Ll(0xe2de7171d971afa8),
            Ll(0x8dc6c8c807c80ecf), Ll(0x32d119196419c87d), Ll(0x923b494939497270), Ll(0xaf5fd9d943d9869a),
            Ll(0xf931f2f2eff2c31d), Ll(0xdba8e3e3abe34b48), Ll(0xb6b95b5b715be22a), Ll(0x0dbc88881a883492),
            Ll(0x293e9a9a529aa4c8), Ll(0x4c0b262698262dbe), Ll(0x64bf3232c8328dfa), Ll(0x7d59b0b0fab0e94a),
            Ll(0xcff2e9e983e91b6a), Ll(0x1e770f0f3c0f7833), Ll(0xb733d5d573d5e6a6), Ll(0x1df480803a8074ba),
            Ll(0x6127bebec2be997c), Ll(0x87ebcdcd13cd26de), Ll(0x68893434d034bde4), Ll(0x903248483d487a75),
            Ll(0xe354ffffdbffab24), Ll(0xf48d7a7af57af78f), Ll(0x3d6490907a90f4ea), Ll(0xbe9d5f5f615fc23e),
            Ll(0x403d202080201da0), Ll(0xd00f6868bd6867d5), Ll(0x34ca1a1a681ad072), Ll(0x41b7aeae82ae192c),
            Ll(0x757db4b4eab4c95e), Ll(0xa8ce54544d549a19), Ll(0x3b7f93937693ece5), Ll(0x442f222288220daa),
            Ll(0xc86364648d6407e9), Ll(0xff2af1f1e3f1db12), Ll(0xe6cc7373d173bfa2), Ll(0x248212124812905a),
            Ll(0x807a40401d403a5d), Ll(0x1048080820084028), Ll(0x9b95c3c32bc356e8), Ll(0xc5dfecec97ec337b),
            Ll(0xab4ddbdb4bdb9690), Ll(0x5fc0a1a1bea1611f), Ll(0x07918d8d0e8d1c83), Ll(0x7ac83d3df43df5c9),
            Ll(0x335b97976697ccf1), Ll(0x0000000000000000), Ll(0x83f9cfcf1bcf36d4), Ll(0x566e2b2bac2b4587),
            Ll(0xece17676c57697b3), Ll(0x19e68282328264b0), Ll(0xb128d6d67fd6fea9), Ll(0x36c31b1b6c1bd877),
            Ll(0x7774b5b5eeb5c15b), Ll(0x43beafaf86af1129), Ll(0xd41d6a6ab56a77df), Ll(0xa0ea50505d50ba0d),
            Ll(0x8a5745450945124c), Ll(0xfb38f3f3ebf3cb18), Ll(0x60ad3030c0309df0), Ll(0xc3c4efef9bef2b74),
            Ll(0x7eda3f3ffc3fe5c3), Ll(0xaac755554955921c), Ll(0x59dba2a2b2a27910), Ll(0xc9e9eaea8fea0365),
            Ll(0xca6a656589650fec), Ll(0x6903babad2bab968), Ll(0x5e4a2f2fbc2f6593), Ll(0x9d8ec0c027c04ee7),
            Ll(0xa160dede5fdebe81), Ll(0x38fc1c1c701ce06c), Ll(0xe746fdfdd3fdbb2e), Ll(0x9a1f4d4d294d5264),
            Ll(0x397692927292e4e0), Ll(0xeafa7575c9758fbc), Ll(0x0c3606061806301e), Ll(0x09ae8a8a128a2498),
            Ll(0x794bb2b2f2b2f940), Ll(0xd185e6e6bfe66359), Ll(0x1c7e0e0e380e7036), Ll(0x3ee71f1f7c1ff863),
            Ll(0xc4556262956237f7), Ll(0xb53ad4d477d4eea3), Ll(0x4d81a8a89aa82932), Ll(0x315296966296c4f4),
            Ll(0xef62f9f9c3f99b3a), Ll(0x97a3c5c533c566f6), Ll(0x4a102525942535b1), Ll(0xb2ab59597959f220),
            Ll(0x15d084842a8454ae), Ll(0xe4c57272d572b7a7), Ll(0x72ec3939e439d5dd), Ll(0x98164c4c2d4c5a61),
            Ll(0xbc945e5e655eca3b), Ll(0xf09f7878fd78e785), Ll(0x70e53838e038ddd8), Ll(0x05988c8c0a8c1486),
            Ll(0xbf17d1d163d1c6b2), Ll(0x57e4a5a5aea5410b), Ll(0xd9a1e2e2afe2434d), Ll(0xc24e616199612ff8),
            Ll(0x7b42b3b3f6b3f145), Ll(0x42342121842115a5), Ll(0x25089c9c4a9c94d6), Ll(0x3cee1e1e781ef066),
            Ll(0x8661434311432252), Ll(0x93b1c7c73bc776fc), Ll(0xe54ffcfcd7fcb32b), Ll(0x0824040410042014),
            Ll(0xa2e351515951b208), Ll(0x2f2599995e99bcc7), Ll(0xda226d6da96d4fc4), Ll(0x1a650d0d340d6839),
            Ll(0xe979fafacffa8335), Ll(0xa369dfdf5bdfb684), Ll(0xfca97e7ee57ed79b), Ll(0x4819242490243db4),
            Ll(0x76fe3b3bec3bc5d7), Ll(0x4b9aabab96ab313d), Ll(0x81f0cece1fce3ed1), Ll(0x2299111144118855),
            Ll(0x03838f8f068f0c89), Ll(0x9c044e4e254e4a6b), Ll(0x7366b7b7e6b7d151), Ll(0xcbe0ebeb8beb0b60),
            Ll(0x78c13c3cf03cfdcc), Ll(0x1ffd81813e817cbf), Ll(0x354094946a94d4fe), Ll(0xf31cf7f7fbf7eb0c),
            Ll(0x6f18b9b9deb9a167), Ll(0x268b13134c13985f), Ll(0x58512c2cb02c7d9c), Ll(0xbb05d3d36bd3d6b8),
            Ll(0xd38ce7e7bbe76b5c), Ll(0xdc396e6ea56e57cb), Ll(0x95aac4c437c46ef3), Ll(0x061b03030c03180f),
            Ll(0xacdc565645568a13), Ll(0x885e44440d441a49), Ll(0xfea07f7fe17fdf9e), Ll(0x4f88a9a99ea92137),
            Ll(0x54672a2aa82a4d82), Ll(0x6b0abbbbd6bbb16d), Ll(0x9f87c1c123c146e2), Ll(0xa6f153535153a202),
            Ll(0xa572dcdc57dcae8b), Ll(0x16530b0b2c0b5827), Ll(0x27019d9d4e9d9cd3), Ll(0xd82b6c6cad6c47c1),
            Ll(0x62a43131c43195f5), Ll(0xe8f37474cd7487b9), Ll(0xf115f6f6fff6e309), Ll(0x8c4c464605460a43),
            Ll(0x45a5acac8aac0926), Ll(0x0fb589891e893c97), Ll(0x28b414145014a044), Ll(0xdfbae1e1a3e15b42),
            Ll(0x2ca616165816b04e), Ll(0x74f73a3ae83acdd2), Ll(0xd2066969b9696fd0), Ll(0x124109092409482d),
            Ll(0xe0d77070dd70a7ad), Ll(0x716fb6b6e2b6d954), Ll(0xbd1ed0d067d0ceb7), Ll(0xc7d6eded93ed3b7e),
            Ll(0x85e2cccc17cc2edb), Ll(0x8468424215422a57), Ll(0x2d2c98985a98b4c2), Ll(0x55eda4a4aaa4490e),
            Ll(0x50752828a0285d88), Ll(0xb8865c5c6d5cda31), Ll(0xed6bf8f8c7f8933f), Ll(0x11c28686228644a4),
        };

        /// <summary>
        /// 
        /// </summary>
        static readonly ulong[] C3 = new ulong[256] {
            Ll(0x7830d818186018c0), Ll(0xaf462623238c2305), Ll(0xf991b8c6c63fc67e), Ll(0x6fcdfbe8e887e813),
            Ll(0xa113cb878726874c), Ll(0x626d11b8b8dab8a9), Ll(0x0502090101040108), Ll(0x6e9e0d4f4f214f42),
            Ll(0xee6c9b3636d836ad), Ll(0x0451ffa6a6a2a659), Ll(0xbdb90cd2d26fd2de), Ll(0x06f70ef5f5f3f5fb),
            Ll(0x80f2967979f979ef), Ll(0xcede306f6fa16f5f), Ll(0xef3f6d91917e91fc), Ll(0x07a4f852525552aa),
            Ll(0xfdc04760609d6027), Ll(0x766535bcbccabc89), Ll(0xcd2b379b9b569bac), Ll(0x8c018a8e8e028e04),
            Ll(0x155bd2a3a3b6a371), Ll(0x3c186c0c0c300c60), Ll(0x8af6847b7bf17bff), Ll(0xe16a803535d435b5),
            Ll(0x693af51d1d741de8), Ll(0x47ddb3e0e0a7e053), Ll(0xacb321d7d77bd7f6), Ll(0xed999cc2c22fc25e),
            Ll(0x965c432e2eb82e6d), Ll(0x7a96294b4b314b62), Ll(0x21e15dfefedffea3), Ll(0x16aed55757415782),
            Ll(0x412abd15155415a8), Ll(0xb6eee87777c1779f), Ll(0xeb6e923737dc37a5), Ll(0x56d79ee5e5b3e57b),
            Ll(0xd923139f9f469f8c), Ll(0x17fd23f0f0e7f0d3), Ll(0x7f94204a4a354a6a), Ll(0x95a944dada4fda9e),
            Ll(0x25b0a258587d58fa), Ll(0xca8fcfc9c903c906), Ll(0x8d527c2929a42955), Ll(0x22145a0a0a280a50),
            Ll(0x4f7f50b1b1feb1e1), Ll(0x1a5dc9a0a0baa069), Ll(0xdad6146b6bb16b7f), Ll(0xab17d985852e855c),
            Ll(0x73673cbdbdcebd81), Ll(0x34ba8f5d5d695dd2), Ll(0x5020901010401080), Ll(0x03f507f4f4f7f4f3),
            Ll(0xc08bddcbcb0bcb16), Ll(0xc67cd33e3ef83eed), Ll(0x110a2d0505140528), Ll(0xe6ce78676781671f),
            Ll(0x53d597e4e4b7e473), Ll(0xbb4e0227279c2725), Ll(0x5882734141194132), Ll(0x9d0ba78b8b168b2c),
            Ll(0x0153f6a7a7a6a751), Ll(0x94fab27d7de97dcf), Ll(0xfb374995956e95dc), Ll(0x9fad56d8d847d88e),
            Ll(0x30eb70fbfbcbfb8b), Ll(0x71c1cdeeee9fee23), Ll(0x91f8bb7c7ced7cc7), Ll(0xe3cc716666856617),
            Ll(0x8ea77bdddd53dda6), Ll(0x4b2eaf17175c17b8), Ll(0x468e454747014702), Ll(0xdc211a9e9e429e84),
            Ll(0xc589d4caca0fca1e), Ll(0x995a582d2db42d75), Ll(0x79632ebfbfc6bf91), Ll(0x1b0e3f07071c0738),
            Ll(0x2347acadad8ead01), Ll(0x2fb4b05a5a755aea), Ll(0xb51bef838336836c), Ll(0xff66b63333cc3385),
            Ll(0xf2c65c636391633f), Ll(0x0a04120202080210), Ll(0x384993aaaa92aa39), Ll(0xa8e2de7171d971af),
            Ll(0xcf8dc6c8c807c80e), Ll(0x7d32d119196419c8), Ll(0x70923b4949394972), Ll(0x9aaf5fd9d943d986),
            Ll(0x1df931f2f2eff2c3), Ll(0x48dba8e3e3abe34b), Ll(0x2ab6b95b5b715be2), Ll(0x920dbc88881a8834),
            Ll(0xc8293e9a9a529aa4), Ll(0xbe4c0b262698262d), Ll(0xfa64bf3232c8328d), Ll(0x4a7d59b0b0fab0e9),
            Ll(0x6acff2e9e983e91b), Ll(0x331e770f0f3c0f78), Ll(0xa6b733d5d573d5e6), Ll(0xba1df480803a8074),
            Ll(0x7c6127bebec2be99), Ll(0xde87ebcdcd13cd26), Ll(0xe468893434d034bd), Ll(0x75903248483d487a),
            Ll(0x24e354ffffdbffab), Ll(0x8ff48d7a7af57af7), Ll(0xea3d6490907a90f4), Ll(0x3ebe9d5f5f615fc2),
            Ll(0xa0403d202080201d), Ll(0xd5d00f6868bd6867), Ll(0x7234ca1a1a681ad0), Ll(0x2c41b7aeae82ae19),
            Ll(0x5e757db4b4eab4c9), Ll(0x19a8ce54544d549a), Ll(0xe53b7f93937693ec), Ll(0xaa442f222288220d),
            Ll(0xe9c86364648d6407), Ll(0x12ff2af1f1e3f1db), Ll(0xa2e6cc7373d173bf), Ll(0x5a24821212481290),
            Ll(0x5d807a40401d403a), Ll(0x2810480808200840), Ll(0xe89b95c3c32bc356), Ll(0x7bc5dfecec97ec33),
            Ll(0x90ab4ddbdb4bdb96), Ll(0x1f5fc0a1a1bea161), Ll(0x8307918d8d0e8d1c), Ll(0xc97ac83d3df43df5),
            Ll(0xf1335b97976697cc), Ll(0x0000000000000000), Ll(0xd483f9cfcf1bcf36), Ll(0x87566e2b2bac2b45),
            Ll(0xb3ece17676c57697), Ll(0xb019e68282328264), Ll(0xa9b128d6d67fd6fe), Ll(0x7736c31b1b6c1bd8),
            Ll(0x5b7774b5b5eeb5c1), Ll(0x2943beafaf86af11), Ll(0xdfd41d6a6ab56a77), Ll(0x0da0ea50505d50ba),
            Ll(0x4c8a574545094512), Ll(0x18fb38f3f3ebf3cb), Ll(0xf060ad3030c0309d), Ll(0x74c3c4efef9bef2b),
            Ll(0xc37eda3f3ffc3fe5), Ll(0x1caac75555495592), Ll(0x1059dba2a2b2a279), Ll(0x65c9e9eaea8fea03),
            Ll(0xecca6a656589650f), Ll(0x686903babad2bab9), Ll(0x935e4a2f2fbc2f65), Ll(0xe79d8ec0c027c04e),
            Ll(0x81a160dede5fdebe), Ll(0x6c38fc1c1c701ce0), Ll(0x2ee746fdfdd3fdbb), Ll(0x649a1f4d4d294d52),
            Ll(0xe0397692927292e4), Ll(0xbceafa7575c9758f), Ll(0x1e0c360606180630), Ll(0x9809ae8a8a128a24),
            Ll(0x40794bb2b2f2b2f9), Ll(0x59d185e6e6bfe663), Ll(0x361c7e0e0e380e70), Ll(0x633ee71f1f7c1ff8),
            Ll(0xf7c4556262956237), Ll(0xa3b53ad4d477d4ee), Ll(0x324d81a8a89aa829), Ll(0xf4315296966296c4),
            Ll(0x3aef62f9f9c3f99b), Ll(0xf697a3c5c533c566), Ll(0xb14a102525942535), Ll(0x20b2ab59597959f2),
            Ll(0xae15d084842a8454), Ll(0xa7e4c57272d572b7), Ll(0xdd72ec3939e439d5), Ll(0x6198164c4c2d4c5a),
            Ll(0x3bbc945e5e655eca), Ll(0x85f09f7878fd78e7), Ll(0xd870e53838e038dd), Ll(0x8605988c8c0a8c14),
            Ll(0xb2bf17d1d163d1c6), Ll(0x0b57e4a5a5aea541), Ll(0x4dd9a1e2e2afe243), Ll(0xf8c24e616199612f),
            Ll(0x457b42b3b3f6b3f1), Ll(0xa542342121842115), Ll(0xd625089c9c4a9c94), Ll(0x663cee1e1e781ef0),
            Ll(0x5286614343114322), Ll(0xfc93b1c7c73bc776), Ll(0x2be54ffcfcd7fcb3), Ll(0x1408240404100420),
            Ll(0x08a2e351515951b2), Ll(0xc72f2599995e99bc), Ll(0xc4da226d6da96d4f), Ll(0x391a650d0d340d68),
            Ll(0x35e979fafacffa83), Ll(0x84a369dfdf5bdfb6), Ll(0x9bfca97e7ee57ed7), Ll(0xb44819242490243d),
            Ll(0xd776fe3b3bec3bc5), Ll(0x3d4b9aabab96ab31), Ll(0xd181f0cece1fce3e), Ll(0x5522991111441188),
            Ll(0x8903838f8f068f0c), Ll(0x6b9c044e4e254e4a), Ll(0x517366b7b7e6b7d1), Ll(0x60cbe0ebeb8beb0b),
            Ll(0xcc78c13c3cf03cfd), Ll(0xbf1ffd81813e817c), Ll(0xfe354094946a94d4), Ll(0x0cf31cf7f7fbf7eb),
            Ll(0x676f18b9b9deb9a1), Ll(0x5f268b13134c1398), Ll(0x9c58512c2cb02c7d), Ll(0xb8bb05d3d36bd3d6),
            Ll(0x5cd38ce7e7bbe76b), Ll(0xcbdc396e6ea56e57), Ll(0xf395aac4c437c46e), Ll(0x0f061b03030c0318),
            Ll(0x13acdc565645568a), Ll(0x49885e44440d441a), Ll(0x9efea07f7fe17fdf), Ll(0x374f88a9a99ea921),
            Ll(0x8254672a2aa82a4d), Ll(0x6d6b0abbbbd6bbb1), Ll(0xe29f87c1c123c146), Ll(0x02a6f153535153a2),
            Ll(0x8ba572dcdc57dcae), Ll(0x2716530b0b2c0b58), Ll(0xd327019d9d4e9d9c), Ll(0xc1d82b6c6cad6c47),
            Ll(0xf562a43131c43195), Ll(0xb9e8f37474cd7487), Ll(0x09f115f6f6fff6e3), Ll(0x438c4c464605460a),
            Ll(0x2645a5acac8aac09), Ll(0x970fb589891e893c), Ll(0x4428b414145014a0), Ll(0x42dfbae1e1a3e15b),
            Ll(0x4e2ca616165816b0), Ll(0xd274f73a3ae83acd), Ll(0xd0d2066969b9696f), Ll(0x2d12410909240948),
            Ll(0xade0d77070dd70a7), Ll(0x54716fb6b6e2b6d9), Ll(0xb7bd1ed0d067d0ce), Ll(0x7ec7d6eded93ed3b),
            Ll(0xdb85e2cccc17cc2e), Ll(0x578468424215422a), Ll(0xc22d2c98985a98b4), Ll(0x0e55eda4a4aaa449),
            Ll(0x8850752828a0285d), Ll(0x31b8865c5c6d5cda), Ll(0x3fed6bf8f8c7f893), Ll(0xa411c28686228644),
        };

        /// <summary>
        /// 
        /// </summary>
        static readonly ulong[] C4 = new ulong[256] {
            Ll(0xc07830d818186018), Ll(0x05af462623238c23), Ll(0x7ef991b8c6c63fc6), Ll(0x136fcdfbe8e887e8),
            Ll(0x4ca113cb87872687), Ll(0xa9626d11b8b8dab8), Ll(0x0805020901010401), Ll(0x426e9e0d4f4f214f),
            Ll(0xadee6c9b3636d836), Ll(0x590451ffa6a6a2a6), Ll(0xdebdb90cd2d26fd2), Ll(0xfb06f70ef5f5f3f5),
            Ll(0xef80f2967979f979), Ll(0x5fcede306f6fa16f), Ll(0xfcef3f6d91917e91), Ll(0xaa07a4f852525552),
            Ll(0x27fdc04760609d60), Ll(0x89766535bcbccabc), Ll(0xaccd2b379b9b569b), Ll(0x048c018a8e8e028e),
            Ll(0x71155bd2a3a3b6a3), Ll(0x603c186c0c0c300c), Ll(0xff8af6847b7bf17b), Ll(0xb5e16a803535d435),
            Ll(0xe8693af51d1d741d), Ll(0x5347ddb3e0e0a7e0), Ll(0xf6acb321d7d77bd7), Ll(0x5eed999cc2c22fc2),
            Ll(0x6d965c432e2eb82e), Ll(0x627a96294b4b314b), Ll(0xa321e15dfefedffe), Ll(0x8216aed557574157),
            Ll(0xa8412abd15155415), Ll(0x9fb6eee87777c177), Ll(0xa5eb6e923737dc37), Ll(0x7b56d79ee5e5b3e5),
            Ll(0x8cd923139f9f469f), Ll(0xd317fd23f0f0e7f0), Ll(0x6a7f94204a4a354a), Ll(0x9e95a944dada4fda),
            Ll(0xfa25b0a258587d58), Ll(0x06ca8fcfc9c903c9), Ll(0x558d527c2929a429), Ll(0x5022145a0a0a280a),
            Ll(0xe14f7f50b1b1feb1), Ll(0x691a5dc9a0a0baa0), Ll(0x7fdad6146b6bb16b), Ll(0x5cab17d985852e85),
            Ll(0x8173673cbdbdcebd), Ll(0xd234ba8f5d5d695d), Ll(0x8050209010104010), Ll(0xf303f507f4f4f7f4),
            Ll(0x16c08bddcbcb0bcb), Ll(0xedc67cd33e3ef83e), Ll(0x28110a2d05051405), Ll(0x1fe6ce7867678167),
            Ll(0x7353d597e4e4b7e4), Ll(0x25bb4e0227279c27), Ll(0x3258827341411941), Ll(0x2c9d0ba78b8b168b),
            Ll(0x510153f6a7a7a6a7), Ll(0xcf94fab27d7de97d), Ll(0xdcfb374995956e95), Ll(0x8e9fad56d8d847d8),
            Ll(0x8b30eb70fbfbcbfb), Ll(0x2371c1cdeeee9fee), Ll(0xc791f8bb7c7ced7c), Ll(0x17e3cc7166668566),
            Ll(0xa68ea77bdddd53dd), Ll(0xb84b2eaf17175c17), Ll(0x02468e4547470147), Ll(0x84dc211a9e9e429e),
            Ll(0x1ec589d4caca0fca), Ll(0x75995a582d2db42d), Ll(0x9179632ebfbfc6bf), Ll(0x381b0e3f07071c07),
            Ll(0x012347acadad8ead), Ll(0xea2fb4b05a5a755a), Ll(0x6cb51bef83833683), Ll(0x85ff66b63333cc33),
            Ll(0x3ff2c65c63639163), Ll(0x100a041202020802), Ll(0x39384993aaaa92aa), Ll(0xafa8e2de7171d971),
            Ll(0x0ecf8dc6c8c807c8), Ll(0xc87d32d119196419), Ll(0x7270923b49493949), Ll(0x869aaf5fd9d943d9),
            Ll(0xc31df931f2f2eff2), Ll(0x4b48dba8e3e3abe3), Ll(0xe22ab6b95b5b715b), Ll(0x34920dbc88881a88),
            Ll(0xa4c8293e9a9a529a), Ll(0x2dbe4c0b26269826), Ll(0x8dfa64bf3232c832), Ll(0xe94a7d59b0b0fab0),
            Ll(0x1b6acff2e9e983e9), Ll(0x78331e770f0f3c0f), Ll(0xe6a6b733d5d573d5), Ll(0x74ba1df480803a80),
            Ll(0x997c6127bebec2be), Ll(0x26de87ebcdcd13cd), Ll(0xbde468893434d034), Ll(0x7a75903248483d48),
            Ll(0xab24e354ffffdbff), Ll(0xf78ff48d7a7af57a), Ll(0xf4ea3d6490907a90), Ll(0xc23ebe9d5f5f615f),
            Ll(0x1da0403d20208020), Ll(0x67d5d00f6868bd68), Ll(0xd07234ca1a1a681a), Ll(0x192c41b7aeae82ae),
            Ll(0xc95e757db4b4eab4), Ll(0x9a19a8ce54544d54), Ll(0xece53b7f93937693), Ll(0x0daa442f22228822),
            Ll(0x07e9c86364648d64), Ll(0xdb12ff2af1f1e3f1), Ll(0xbfa2e6cc7373d173), Ll(0x905a248212124812),
            Ll(0x3a5d807a40401d40), Ll(0x4028104808082008), Ll(0x56e89b95c3c32bc3), Ll(0x337bc5dfecec97ec),
            Ll(0x9690ab4ddbdb4bdb), Ll(0x611f5fc0a1a1bea1), Ll(0x1c8307918d8d0e8d), Ll(0xf5c97ac83d3df43d),
            Ll(0xccf1335b97976697), Ll(0x0000000000000000), Ll(0x36d483f9cfcf1bcf), Ll(0x4587566e2b2bac2b),
            Ll(0x97b3ece17676c576), Ll(0x64b019e682823282), Ll(0xfea9b128d6d67fd6), Ll(0xd87736c31b1b6c1b),
            Ll(0xc15b7774b5b5eeb5), Ll(0x112943beafaf86af), Ll(0x77dfd41d6a6ab56a), Ll(0xba0da0ea50505d50),
            Ll(0x124c8a5745450945), Ll(0xcb18fb38f3f3ebf3), Ll(0x9df060ad3030c030), Ll(0x2b74c3c4efef9bef),
            Ll(0xe5c37eda3f3ffc3f), Ll(0x921caac755554955), Ll(0x791059dba2a2b2a2), Ll(0x0365c9e9eaea8fea),
            Ll(0x0fecca6a65658965), Ll(0xb9686903babad2ba), Ll(0x65935e4a2f2fbc2f), Ll(0x4ee79d8ec0c027c0),
            Ll(0xbe81a160dede5fde), Ll(0xe06c38fc1c1c701c), Ll(0xbb2ee746fdfdd3fd), Ll(0x52649a1f4d4d294d),
            Ll(0xe4e0397692927292), Ll(0x8fbceafa7575c975), Ll(0x301e0c3606061806), Ll(0x249809ae8a8a128a),
            Ll(0xf940794bb2b2f2b2), Ll(0x6359d185e6e6bfe6), Ll(0x70361c7e0e0e380e), Ll(0xf8633ee71f1f7c1f),
            Ll(0x37f7c45562629562), Ll(0xeea3b53ad4d477d4), Ll(0x29324d81a8a89aa8), Ll(0xc4f4315296966296),
            Ll(0x9b3aef62f9f9c3f9), Ll(0x66f697a3c5c533c5), Ll(0x35b14a1025259425), Ll(0xf220b2ab59597959),
            Ll(0x54ae15d084842a84), Ll(0xb7a7e4c57272d572), Ll(0xd5dd72ec3939e439), Ll(0x5a6198164c4c2d4c),
            Ll(0xca3bbc945e5e655e), Ll(0xe785f09f7878fd78), Ll(0xddd870e53838e038), Ll(0x148605988c8c0a8c),
            Ll(0xc6b2bf17d1d163d1), Ll(0x410b57e4a5a5aea5), Ll(0x434dd9a1e2e2afe2), Ll(0x2ff8c24e61619961),
            Ll(0xf1457b42b3b3f6b3), Ll(0x15a5423421218421), Ll(0x94d625089c9c4a9c), Ll(0xf0663cee1e1e781e),
            Ll(0x2252866143431143), Ll(0x76fc93b1c7c73bc7), Ll(0xb32be54ffcfcd7fc), Ll(0x2014082404041004),
            Ll(0xb208a2e351515951), Ll(0xbcc72f2599995e99), Ll(0x4fc4da226d6da96d), Ll(0x68391a650d0d340d),
            Ll(0x8335e979fafacffa), Ll(0xb684a369dfdf5bdf), Ll(0xd79bfca97e7ee57e), Ll(0x3db4481924249024),
            Ll(0xc5d776fe3b3bec3b), Ll(0x313d4b9aabab96ab), Ll(0x3ed181f0cece1fce), Ll(0x8855229911114411),
            Ll(0x0c8903838f8f068f), Ll(0x4a6b9c044e4e254e), Ll(0xd1517366b7b7e6b7), Ll(0x0b60cbe0ebeb8beb),
            Ll(0xfdcc78c13c3cf03c), Ll(0x7cbf1ffd81813e81), Ll(0xd4fe354094946a94), Ll(0xeb0cf31cf7f7fbf7),
            Ll(0xa1676f18b9b9deb9), Ll(0x985f268b13134c13), Ll(0x7d9c58512c2cb02c), Ll(0xd6b8bb05d3d36bd3),
            Ll(0x6b5cd38ce7e7bbe7), Ll(0x57cbdc396e6ea56e), Ll(0x6ef395aac4c437c4), Ll(0x180f061b03030c03),
            Ll(0x8a13acdc56564556), Ll(0x1a49885e44440d44), Ll(0xdf9efea07f7fe17f), Ll(0x21374f88a9a99ea9),
            Ll(0x4d8254672a2aa82a), Ll(0xb16d6b0abbbbd6bb), Ll(0x46e29f87c1c123c1), Ll(0xa202a6f153535153),
            Ll(0xae8ba572dcdc57dc), Ll(0x582716530b0b2c0b), Ll(0x9cd327019d9d4e9d), Ll(0x47c1d82b6c6cad6c),
            Ll(0x95f562a43131c431), Ll(0x87b9e8f37474cd74), Ll(0xe309f115f6f6fff6), Ll(0x0a438c4c46460546),
            Ll(0x092645a5acac8aac), Ll(0x3c970fb589891e89), Ll(0xa04428b414145014), Ll(0x5b42dfbae1e1a3e1),
            Ll(0xb04e2ca616165816), Ll(0xcdd274f73a3ae83a), Ll(0x6fd0d2066969b969), Ll(0x482d124109092409),
            Ll(0xa7ade0d77070dd70), Ll(0xd954716fb6b6e2b6), Ll(0xceb7bd1ed0d067d0), Ll(0x3b7ec7d6eded93ed),
            Ll(0x2edb85e2cccc17cc), Ll(0x2a57846842421542), Ll(0xb4c22d2c98985a98), Ll(0x490e55eda4a4aaa4),
            Ll(0x5d8850752828a028), Ll(0xda31b8865c5c6d5c), Ll(0x933fed6bf8f8c7f8), Ll(0x44a411c286862286),
        };

        /// <summary>
        /// 
        /// </summary>
        static readonly ulong[] C5 = new ulong[256] {
            Ll(0x18c07830d8181860), Ll(0x2305af462623238c), Ll(0xc67ef991b8c6c63f), Ll(0xe8136fcdfbe8e887),
            Ll(0x874ca113cb878726), Ll(0xb8a9626d11b8b8da), Ll(0x0108050209010104), Ll(0x4f426e9e0d4f4f21),
            Ll(0x36adee6c9b3636d8), Ll(0xa6590451ffa6a6a2), Ll(0xd2debdb90cd2d26f), Ll(0xf5fb06f70ef5f5f3),
            Ll(0x79ef80f2967979f9), Ll(0x6f5fcede306f6fa1), Ll(0x91fcef3f6d91917e), Ll(0x52aa07a4f8525255),
            Ll(0x6027fdc04760609d), Ll(0xbc89766535bcbcca), Ll(0x9baccd2b379b9b56), Ll(0x8e048c018a8e8e02),
            Ll(0xa371155bd2a3a3b6), Ll(0x0c603c186c0c0c30), Ll(0x7bff8af6847b7bf1), Ll(0x35b5e16a803535d4),
            Ll(0x1de8693af51d1d74), Ll(0xe05347ddb3e0e0a7), Ll(0xd7f6acb321d7d77b), Ll(0xc25eed999cc2c22f),
            Ll(0x2e6d965c432e2eb8), Ll(0x4b627a96294b4b31), Ll(0xfea321e15dfefedf), Ll(0x578216aed5575741),
            Ll(0x15a8412abd151554), Ll(0x779fb6eee87777c1), Ll(0x37a5eb6e923737dc), Ll(0xe57b56d79ee5e5b3),
            Ll(0x9f8cd923139f9f46), Ll(0xf0d317fd23f0f0e7), Ll(0x4a6a7f94204a4a35), Ll(0xda9e95a944dada4f),
            Ll(0x58fa25b0a258587d), Ll(0xc906ca8fcfc9c903), Ll(0x29558d527c2929a4), Ll(0x0a5022145a0a0a28),
            Ll(0xb1e14f7f50b1b1fe), Ll(0xa0691a5dc9a0a0ba), Ll(0x6b7fdad6146b6bb1), Ll(0x855cab17d985852e),
            Ll(0xbd8173673cbdbdce), Ll(0x5dd234ba8f5d5d69), Ll(0x1080502090101040), Ll(0xf4f303f507f4f4f7),
            Ll(0xcb16c08bddcbcb0b), Ll(0x3eedc67cd33e3ef8), Ll(0x0528110a2d050514), Ll(0x671fe6ce78676781),
            Ll(0xe47353d597e4e4b7), Ll(0x2725bb4e0227279c), Ll(0x4132588273414119), Ll(0x8b2c9d0ba78b8b16),
            Ll(0xa7510153f6a7a7a6), Ll(0x7dcf94fab27d7de9), Ll(0x95dcfb374995956e), Ll(0xd88e9fad56d8d847),
            Ll(0xfb8b30eb70fbfbcb), Ll(0xee2371c1cdeeee9f), Ll(0x7cc791f8bb7c7ced), Ll(0x6617e3cc71666685),
            Ll(0xdda68ea77bdddd53), Ll(0x17b84b2eaf17175c), Ll(0x4702468e45474701), Ll(0x9e84dc211a9e9e42),
            Ll(0xca1ec589d4caca0f), Ll(0x2d75995a582d2db4), Ll(0xbf9179632ebfbfc6), Ll(0x07381b0e3f07071c),
            Ll(0xad012347acadad8e), Ll(0x5aea2fb4b05a5a75), Ll(0x836cb51bef838336), Ll(0x3385ff66b63333cc),
            Ll(0x633ff2c65c636391), Ll(0x02100a0412020208), Ll(0xaa39384993aaaa92), Ll(0x71afa8e2de7171d9),
            Ll(0xc80ecf8dc6c8c807), Ll(0x19c87d32d1191964), Ll(0x497270923b494939), Ll(0xd9869aaf5fd9d943),
            Ll(0xf2c31df931f2f2ef), Ll(0xe34b48dba8e3e3ab), Ll(0x5be22ab6b95b5b71), Ll(0x8834920dbc88881a),
            Ll(0x9aa4c8293e9a9a52), Ll(0x262dbe4c0b262698), Ll(0x328dfa64bf3232c8), Ll(0xb0e94a7d59b0b0fa),
            Ll(0xe91b6acff2e9e983), Ll(0x0f78331e770f0f3c), Ll(0xd5e6a6b733d5d573), Ll(0x8074ba1df480803a),
            Ll(0xbe997c6127bebec2), Ll(0xcd26de87ebcdcd13), Ll(0x34bde468893434d0), Ll(0x487a75903248483d),
            Ll(0xffab24e354ffffdb), Ll(0x7af78ff48d7a7af5), Ll(0x90f4ea3d6490907a), Ll(0x5fc23ebe9d5f5f61),
            Ll(0x201da0403d202080), Ll(0x6867d5d00f6868bd), Ll(0x1ad07234ca1a1a68), Ll(0xae192c41b7aeae82),
            Ll(0xb4c95e757db4b4ea), Ll(0x549a19a8ce54544d), Ll(0x93ece53b7f939376), Ll(0x220daa442f222288),
            Ll(0x6407e9c86364648d), Ll(0xf1db12ff2af1f1e3), Ll(0x73bfa2e6cc7373d1), Ll(0x12905a2482121248),
            Ll(0x403a5d807a40401d), Ll(0x0840281048080820), Ll(0xc356e89b95c3c32b), Ll(0xec337bc5dfecec97),
            Ll(0xdb9690ab4ddbdb4b), Ll(0xa1611f5fc0a1a1be), Ll(0x8d1c8307918d8d0e), Ll(0x3df5c97ac83d3df4),
            Ll(0x97ccf1335b979766), Ll(0x0000000000000000), Ll(0xcf36d483f9cfcf1b), Ll(0x2b4587566e2b2bac),
            Ll(0x7697b3ece17676c5), Ll(0x8264b019e6828232), Ll(0xd6fea9b128d6d67f), Ll(0x1bd87736c31b1b6c),
            Ll(0xb5c15b7774b5b5ee), Ll(0xaf112943beafaf86), Ll(0x6a77dfd41d6a6ab5), Ll(0x50ba0da0ea50505d),
            Ll(0x45124c8a57454509), Ll(0xf3cb18fb38f3f3eb), Ll(0x309df060ad3030c0), Ll(0xef2b74c3c4efef9b),
            Ll(0x3fe5c37eda3f3ffc), Ll(0x55921caac7555549), Ll(0xa2791059dba2a2b2), Ll(0xea0365c9e9eaea8f),
            Ll(0x650fecca6a656589), Ll(0xbab9686903babad2), Ll(0x2f65935e4a2f2fbc), Ll(0xc04ee79d8ec0c027),
            Ll(0xdebe81a160dede5f), Ll(0x1ce06c38fc1c1c70), Ll(0xfdbb2ee746fdfdd3), Ll(0x4d52649a1f4d4d29),
            Ll(0x92e4e03976929272), Ll(0x758fbceafa7575c9), Ll(0x06301e0c36060618), Ll(0x8a249809ae8a8a12),
            Ll(0xb2f940794bb2b2f2), Ll(0xe66359d185e6e6bf), Ll(0x0e70361c7e0e0e38), Ll(0x1ff8633ee71f1f7c),
            Ll(0x6237f7c455626295), Ll(0xd4eea3b53ad4d477), Ll(0xa829324d81a8a89a), Ll(0x96c4f43152969662),
            Ll(0xf99b3aef62f9f9c3), Ll(0xc566f697a3c5c533), Ll(0x2535b14a10252594), Ll(0x59f220b2ab595979),
            Ll(0x8454ae15d084842a), Ll(0x72b7a7e4c57272d5), Ll(0x39d5dd72ec3939e4), Ll(0x4c5a6198164c4c2d),
            Ll(0x5eca3bbc945e5e65), Ll(0x78e785f09f7878fd), Ll(0x38ddd870e53838e0), Ll(0x8c148605988c8c0a),
            Ll(0xd1c6b2bf17d1d163), Ll(0xa5410b57e4a5a5ae), Ll(0xe2434dd9a1e2e2af), Ll(0x612ff8c24e616199),
            Ll(0xb3f1457b42b3b3f6), Ll(0x2115a54234212184), Ll(0x9c94d625089c9c4a), Ll(0x1ef0663cee1e1e78),
            Ll(0x4322528661434311), Ll(0xc776fc93b1c7c73b), Ll(0xfcb32be54ffcfcd7), Ll(0x0420140824040410),
            Ll(0x51b208a2e3515159), Ll(0x99bcc72f2599995e), Ll(0x6d4fc4da226d6da9), Ll(0x0d68391a650d0d34),
            Ll(0xfa8335e979fafacf), Ll(0xdfb684a369dfdf5b), Ll(0x7ed79bfca97e7ee5), Ll(0x243db44819242490),
            Ll(0x3bc5d776fe3b3bec), Ll(0xab313d4b9aabab96), Ll(0xce3ed181f0cece1f), Ll(0x1188552299111144),
            Ll(0x8f0c8903838f8f06), Ll(0x4e4a6b9c044e4e25), Ll(0xb7d1517366b7b7e6), Ll(0xeb0b60cbe0ebeb8b),
            Ll(0x3cfdcc78c13c3cf0), Ll(0x817cbf1ffd81813e), Ll(0x94d4fe354094946a), Ll(0xf7eb0cf31cf7f7fb),
            Ll(0xb9a1676f18b9b9de), Ll(0x13985f268b13134c), Ll(0x2c7d9c58512c2cb0), Ll(0xd3d6b8bb05d3d36b),
            Ll(0xe76b5cd38ce7e7bb), Ll(0x6e57cbdc396e6ea5), Ll(0xc46ef395aac4c437), Ll(0x03180f061b03030c),
            Ll(0x568a13acdc565645), Ll(0x441a49885e44440d), Ll(0x7fdf9efea07f7fe1), Ll(0xa921374f88a9a99e),
            Ll(0x2a4d8254672a2aa8), Ll(0xbbb16d6b0abbbbd6), Ll(0xc146e29f87c1c123), Ll(0x53a202a6f1535351),
            Ll(0xdcae8ba572dcdc57), Ll(0x0b582716530b0b2c), Ll(0x9d9cd327019d9d4e), Ll(0x6c47c1d82b6c6cad),
            Ll(0x3195f562a43131c4), Ll(0x7487b9e8f37474cd), Ll(0xf6e309f115f6f6ff), Ll(0x460a438c4c464605),
            Ll(0xac092645a5acac8a), Ll(0x893c970fb589891e), Ll(0x14a04428b4141450), Ll(0xe15b42dfbae1e1a3),
            Ll(0x16b04e2ca6161658), Ll(0x3acdd274f73a3ae8), Ll(0x696fd0d2066969b9), Ll(0x09482d1241090924),
            Ll(0x70a7ade0d77070dd), Ll(0xb6d954716fb6b6e2), Ll(0xd0ceb7bd1ed0d067), Ll(0xed3b7ec7d6eded93),
            Ll(0xcc2edb85e2cccc17), Ll(0x422a578468424215), Ll(0x98b4c22d2c98985a), Ll(0xa4490e55eda4a4aa),
            Ll(0x285d8850752828a0), Ll(0x5cda31b8865c5c6d), Ll(0xf8933fed6bf8f8c7), Ll(0x8644a411c2868622),
        };

        /// <summary>
        /// 
        /// </summary>
        static readonly ulong[] C6 = new ulong[256] {
            Ll(0x6018c07830d81818), Ll(0x8c2305af46262323), Ll(0x3fc67ef991b8c6c6), Ll(0x87e8136fcdfbe8e8),
            Ll(0x26874ca113cb8787), Ll(0xdab8a9626d11b8b8), Ll(0x0401080502090101), Ll(0x214f426e9e0d4f4f),
            Ll(0xd836adee6c9b3636), Ll(0xa2a6590451ffa6a6), Ll(0x6fd2debdb90cd2d2), Ll(0xf3f5fb06f70ef5f5),
            Ll(0xf979ef80f2967979), Ll(0xa16f5fcede306f6f), Ll(0x7e91fcef3f6d9191), Ll(0x5552aa07a4f85252),
            Ll(0x9d6027fdc0476060), Ll(0xcabc89766535bcbc), Ll(0x569baccd2b379b9b), Ll(0x028e048c018a8e8e),
            Ll(0xb6a371155bd2a3a3), Ll(0x300c603c186c0c0c), Ll(0xf17bff8af6847b7b), Ll(0xd435b5e16a803535),
            Ll(0x741de8693af51d1d), Ll(0xa7e05347ddb3e0e0), Ll(0x7bd7f6acb321d7d7), Ll(0x2fc25eed999cc2c2),
            Ll(0xb82e6d965c432e2e), Ll(0x314b627a96294b4b), Ll(0xdffea321e15dfefe), Ll(0x41578216aed55757),
            Ll(0x5415a8412abd1515), Ll(0xc1779fb6eee87777), Ll(0xdc37a5eb6e923737), Ll(0xb3e57b56d79ee5e5),
            Ll(0x469f8cd923139f9f), Ll(0xe7f0d317fd23f0f0), Ll(0x354a6a7f94204a4a), Ll(0x4fda9e95a944dada),
            Ll(0x7d58fa25b0a25858), Ll(0x03c906ca8fcfc9c9), Ll(0xa429558d527c2929), Ll(0x280a5022145a0a0a),
            Ll(0xfeb1e14f7f50b1b1), Ll(0xbaa0691a5dc9a0a0), Ll(0xb16b7fdad6146b6b), Ll(0x2e855cab17d98585),
            Ll(0xcebd8173673cbdbd), Ll(0x695dd234ba8f5d5d), Ll(0x4010805020901010), Ll(0xf7f4f303f507f4f4),
            Ll(0x0bcb16c08bddcbcb), Ll(0xf83eedc67cd33e3e), Ll(0x140528110a2d0505), Ll(0x81671fe6ce786767),
            Ll(0xb7e47353d597e4e4), Ll(0x9c2725bb4e022727), Ll(0x1941325882734141), Ll(0x168b2c9d0ba78b8b),
            Ll(0xa6a7510153f6a7a7), Ll(0xe97dcf94fab27d7d), Ll(0x6e95dcfb37499595), Ll(0x47d88e9fad56d8d8),
            Ll(0xcbfb8b30eb70fbfb), Ll(0x9fee2371c1cdeeee), Ll(0xed7cc791f8bb7c7c), Ll(0x856617e3cc716666),
            Ll(0x53dda68ea77bdddd), Ll(0x5c17b84b2eaf1717), Ll(0x014702468e454747), Ll(0x429e84dc211a9e9e),
            Ll(0x0fca1ec589d4caca), Ll(0xb42d75995a582d2d), Ll(0xc6bf9179632ebfbf), Ll(0x1c07381b0e3f0707),
            Ll(0x8ead012347acadad), Ll(0x755aea2fb4b05a5a), Ll(0x36836cb51bef8383), Ll(0xcc3385ff66b63333),
            Ll(0x91633ff2c65c6363), Ll(0x0802100a04120202), Ll(0x92aa39384993aaaa), Ll(0xd971afa8e2de7171),
            Ll(0x07c80ecf8dc6c8c8), Ll(0x6419c87d32d11919), Ll(0x39497270923b4949), Ll(0x43d9869aaf5fd9d9),
            Ll(0xeff2c31df931f2f2), Ll(0xabe34b48dba8e3e3), Ll(0x715be22ab6b95b5b), Ll(0x1a8834920dbc8888),
            Ll(0x529aa4c8293e9a9a), Ll(0x98262dbe4c0b2626), Ll(0xc8328dfa64bf3232), Ll(0xfab0e94a7d59b0b0),
            Ll(0x83e91b6acff2e9e9), Ll(0x3c0f78331e770f0f), Ll(0x73d5e6a6b733d5d5), Ll(0x3a8074ba1df48080),
            Ll(0xc2be997c6127bebe), Ll(0x13cd26de87ebcdcd), Ll(0xd034bde468893434), Ll(0x3d487a7590324848),
            Ll(0xdbffab24e354ffff), Ll(0xf57af78ff48d7a7a), Ll(0x7a90f4ea3d649090), Ll(0x615fc23ebe9d5f5f),
            Ll(0x80201da0403d2020), Ll(0xbd6867d5d00f6868), Ll(0x681ad07234ca1a1a), Ll(0x82ae192c41b7aeae),
            Ll(0xeab4c95e757db4b4), Ll(0x4d549a19a8ce5454), Ll(0x7693ece53b7f9393), Ll(0x88220daa442f2222),
            Ll(0x8d6407e9c8636464), Ll(0xe3f1db12ff2af1f1), Ll(0xd173bfa2e6cc7373), Ll(0x4812905a24821212),
            Ll(0x1d403a5d807a4040), Ll(0x2008402810480808), Ll(0x2bc356e89b95c3c3), Ll(0x97ec337bc5dfecec),
            Ll(0x4bdb9690ab4ddbdb), Ll(0xbea1611f5fc0a1a1), Ll(0x0e8d1c8307918d8d), Ll(0xf43df5c97ac83d3d),
            Ll(0x6697ccf1335b9797), Ll(0x0000000000000000), Ll(0x1bcf36d483f9cfcf), Ll(0xac2b4587566e2b2b),
            Ll(0xc57697b3ece17676), Ll(0x328264b019e68282), Ll(0x7fd6fea9b128d6d6), Ll(0x6c1bd87736c31b1b),
            Ll(0xeeb5c15b7774b5b5), Ll(0x86af112943beafaf), Ll(0xb56a77dfd41d6a6a), Ll(0x5d50ba0da0ea5050),
            Ll(0x0945124c8a574545), Ll(0xebf3cb18fb38f3f3), Ll(0xc0309df060ad3030), Ll(0x9bef2b74c3c4efef),
            Ll(0xfc3fe5c37eda3f3f), Ll(0x4955921caac75555), Ll(0xb2a2791059dba2a2), Ll(0x8fea0365c9e9eaea),
            Ll(0x89650fecca6a6565), Ll(0xd2bab9686903baba), Ll(0xbc2f65935e4a2f2f), Ll(0x27c04ee79d8ec0c0),
            Ll(0x5fdebe81a160dede), Ll(0x701ce06c38fc1c1c), Ll(0xd3fdbb2ee746fdfd), Ll(0x294d52649a1f4d4d),
            Ll(0x7292e4e039769292), Ll(0xc9758fbceafa7575), Ll(0x1806301e0c360606), Ll(0x128a249809ae8a8a),
            Ll(0xf2b2f940794bb2b2), Ll(0xbfe66359d185e6e6), Ll(0x380e70361c7e0e0e), Ll(0x7c1ff8633ee71f1f),
            Ll(0x956237f7c4556262), Ll(0x77d4eea3b53ad4d4), Ll(0x9aa829324d81a8a8), Ll(0x6296c4f431529696),
            Ll(0xc3f99b3aef62f9f9), Ll(0x33c566f697a3c5c5), Ll(0x942535b14a102525), Ll(0x7959f220b2ab5959),
            Ll(0x2a8454ae15d08484), Ll(0xd572b7a7e4c57272), Ll(0xe439d5dd72ec3939), Ll(0x2d4c5a6198164c4c),
            Ll(0x655eca3bbc945e5e), Ll(0xfd78e785f09f7878), Ll(0xe038ddd870e53838), Ll(0x0a8c148605988c8c),
            Ll(0x63d1c6b2bf17d1d1), Ll(0xaea5410b57e4a5a5), Ll(0xafe2434dd9a1e2e2), Ll(0x99612ff8c24e6161),
            Ll(0xf6b3f1457b42b3b3), Ll(0x842115a542342121), Ll(0x4a9c94d625089c9c), Ll(0x781ef0663cee1e1e),
            Ll(0x1143225286614343), Ll(0x3bc776fc93b1c7c7), Ll(0xd7fcb32be54ffcfc), Ll(0x1004201408240404),
            Ll(0x5951b208a2e35151), Ll(0x5e99bcc72f259999), Ll(0xa96d4fc4da226d6d), Ll(0x340d68391a650d0d),
            Ll(0xcffa8335e979fafa), Ll(0x5bdfb684a369dfdf), Ll(0xe57ed79bfca97e7e), Ll(0x90243db448192424),
            Ll(0xec3bc5d776fe3b3b), Ll(0x96ab313d4b9aabab), Ll(0x1fce3ed181f0cece), Ll(0x4411885522991111),
            Ll(0x068f0c8903838f8f), Ll(0x254e4a6b9c044e4e), Ll(0xe6b7d1517366b7b7), Ll(0x8beb0b60cbe0ebeb),
            Ll(0xf03cfdcc78c13c3c), Ll(0x3e817cbf1ffd8181), Ll(0x6a94d4fe35409494), Ll(0xfbf7eb0cf31cf7f7),
            Ll(0xdeb9a1676f18b9b9), Ll(0x4c13985f268b1313), Ll(0xb02c7d9c58512c2c), Ll(0x6bd3d6b8bb05d3d3),
            Ll(0xbbe76b5cd38ce7e7), Ll(0xa56e57cbdc396e6e), Ll(0x37c46ef395aac4c4), Ll(0x0c03180f061b0303),
            Ll(0x45568a13acdc5656), Ll(0x0d441a49885e4444), Ll(0xe17fdf9efea07f7f), Ll(0x9ea921374f88a9a9),
            Ll(0xa82a4d8254672a2a), Ll(0xd6bbb16d6b0abbbb), Ll(0x23c146e29f87c1c1), Ll(0x5153a202a6f15353),
            Ll(0x57dcae8ba572dcdc), Ll(0x2c0b582716530b0b), Ll(0x4e9d9cd327019d9d), Ll(0xad6c47c1d82b6c6c),
            Ll(0xc43195f562a43131), Ll(0xcd7487b9e8f37474), Ll(0xfff6e309f115f6f6), Ll(0x05460a438c4c4646),
            Ll(0x8aac092645a5acac), Ll(0x1e893c970fb58989), Ll(0x5014a04428b41414), Ll(0xa3e15b42dfbae1e1),
            Ll(0x5816b04e2ca61616), Ll(0xe83acdd274f73a3a), Ll(0xb9696fd0d2066969), Ll(0x2409482d12410909),
            Ll(0xdd70a7ade0d77070), Ll(0xe2b6d954716fb6b6), Ll(0x67d0ceb7bd1ed0d0), Ll(0x93ed3b7ec7d6eded),
            Ll(0x17cc2edb85e2cccc), Ll(0x15422a5784684242), Ll(0x5a98b4c22d2c9898), Ll(0xaaa4490e55eda4a4),
            Ll(0xa0285d8850752828), Ll(0x6d5cda31b8865c5c), Ll(0xc7f8933fed6bf8f8), Ll(0x228644a411c28686),
        };

        /// <summary>
        /// 
        /// </summary>
        static readonly ulong[] C7 = new ulong[256] {
            Ll(0x186018c07830d818), Ll(0x238c2305af462623), Ll(0xc63fc67ef991b8c6), Ll(0xe887e8136fcdfbe8),
            Ll(0x8726874ca113cb87), Ll(0xb8dab8a9626d11b8), Ll(0x0104010805020901), Ll(0x4f214f426e9e0d4f),
            Ll(0x36d836adee6c9b36), Ll(0xa6a2a6590451ffa6), Ll(0xd26fd2debdb90cd2), Ll(0xf5f3f5fb06f70ef5),
            Ll(0x79f979ef80f29679), Ll(0x6fa16f5fcede306f), Ll(0x917e91fcef3f6d91), Ll(0x525552aa07a4f852),
            Ll(0x609d6027fdc04760), Ll(0xbccabc89766535bc), Ll(0x9b569baccd2b379b), Ll(0x8e028e048c018a8e),
            Ll(0xa3b6a371155bd2a3), Ll(0x0c300c603c186c0c), Ll(0x7bf17bff8af6847b), Ll(0x35d435b5e16a8035),
            Ll(0x1d741de8693af51d), Ll(0xe0a7e05347ddb3e0), Ll(0xd77bd7f6acb321d7), Ll(0xc22fc25eed999cc2),
            Ll(0x2eb82e6d965c432e), Ll(0x4b314b627a96294b), Ll(0xfedffea321e15dfe), Ll(0x5741578216aed557),
            Ll(0x155415a8412abd15), Ll(0x77c1779fb6eee877), Ll(0x37dc37a5eb6e9237), Ll(0xe5b3e57b56d79ee5),
            Ll(0x9f469f8cd923139f), Ll(0xf0e7f0d317fd23f0), Ll(0x4a354a6a7f94204a), Ll(0xda4fda9e95a944da),
            Ll(0x587d58fa25b0a258), Ll(0xc903c906ca8fcfc9), Ll(0x29a429558d527c29), Ll(0x0a280a5022145a0a),
            Ll(0xb1feb1e14f7f50b1), Ll(0xa0baa0691a5dc9a0), Ll(0x6bb16b7fdad6146b), Ll(0x852e855cab17d985),
            Ll(0xbdcebd8173673cbd), Ll(0x5d695dd234ba8f5d), Ll(0x1040108050209010), Ll(0xf4f7f4f303f507f4),
            Ll(0xcb0bcb16c08bddcb), Ll(0x3ef83eedc67cd33e), Ll(0x05140528110a2d05), Ll(0x6781671fe6ce7867),
            Ll(0xe4b7e47353d597e4), Ll(0x279c2725bb4e0227), Ll(0x4119413258827341), Ll(0x8b168b2c9d0ba78b),
            Ll(0xa7a6a7510153f6a7), Ll(0x7de97dcf94fab27d), Ll(0x956e95dcfb374995), Ll(0xd847d88e9fad56d8),
            Ll(0xfbcbfb8b30eb70fb), Ll(0xee9fee2371c1cdee), Ll(0x7ced7cc791f8bb7c), Ll(0x66856617e3cc7166),
            Ll(0xdd53dda68ea77bdd), Ll(0x175c17b84b2eaf17), Ll(0x47014702468e4547), Ll(0x9e429e84dc211a9e),
            Ll(0xca0fca1ec589d4ca), Ll(0x2db42d75995a582d), Ll(0xbfc6bf9179632ebf), Ll(0x071c07381b0e3f07),
            Ll(0xad8ead012347acad), Ll(0x5a755aea2fb4b05a), Ll(0x8336836cb51bef83), Ll(0x33cc3385ff66b633),
            Ll(0x6391633ff2c65c63), Ll(0x020802100a041202), Ll(0xaa92aa39384993aa), Ll(0x71d971afa8e2de71),
            Ll(0xc807c80ecf8dc6c8), Ll(0x196419c87d32d119), Ll(0x4939497270923b49), Ll(0xd943d9869aaf5fd9),
            Ll(0xf2eff2c31df931f2), Ll(0xe3abe34b48dba8e3), Ll(0x5b715be22ab6b95b), Ll(0x881a8834920dbc88),
            Ll(0x9a529aa4c8293e9a), Ll(0x2698262dbe4c0b26), Ll(0x32c8328dfa64bf32), Ll(0xb0fab0e94a7d59b0),
            Ll(0xe983e91b6acff2e9), Ll(0x0f3c0f78331e770f), Ll(0xd573d5e6a6b733d5), Ll(0x803a8074ba1df480),
            Ll(0xbec2be997c6127be), Ll(0xcd13cd26de87ebcd), Ll(0x34d034bde4688934), Ll(0x483d487a75903248),
            Ll(0xffdbffab24e354ff), Ll(0x7af57af78ff48d7a), Ll(0x907a90f4ea3d6490), Ll(0x5f615fc23ebe9d5f),
            Ll(0x2080201da0403d20), Ll(0x68bd6867d5d00f68), Ll(0x1a681ad07234ca1a), Ll(0xae82ae192c41b7ae),
            Ll(0xb4eab4c95e757db4), Ll(0x544d549a19a8ce54), Ll(0x937693ece53b7f93), Ll(0x2288220daa442f22),
            Ll(0x648d6407e9c86364), Ll(0xf1e3f1db12ff2af1), Ll(0x73d173bfa2e6cc73), Ll(0x124812905a248212),
            Ll(0x401d403a5d807a40), Ll(0x0820084028104808), Ll(0xc32bc356e89b95c3), Ll(0xec97ec337bc5dfec),
            Ll(0xdb4bdb9690ab4ddb), Ll(0xa1bea1611f5fc0a1), Ll(0x8d0e8d1c8307918d), Ll(0x3df43df5c97ac83d),
            Ll(0x976697ccf1335b97), Ll(0x0000000000000000), Ll(0xcf1bcf36d483f9cf), Ll(0x2bac2b4587566e2b),
            Ll(0x76c57697b3ece176), Ll(0x82328264b019e682), Ll(0xd67fd6fea9b128d6), Ll(0x1b6c1bd87736c31b),
            Ll(0xb5eeb5c15b7774b5), Ll(0xaf86af112943beaf), Ll(0x6ab56a77dfd41d6a), Ll(0x505d50ba0da0ea50),
            Ll(0x450945124c8a5745), Ll(0xf3ebf3cb18fb38f3), Ll(0x30c0309df060ad30), Ll(0xef9bef2b74c3c4ef),
            Ll(0x3ffc3fe5c37eda3f), Ll(0x554955921caac755), Ll(0xa2b2a2791059dba2), Ll(0xea8fea0365c9e9ea),
            Ll(0x6589650fecca6a65), Ll(0xbad2bab9686903ba), Ll(0x2fbc2f65935e4a2f), Ll(0xc027c04ee79d8ec0),
            Ll(0xde5fdebe81a160de), Ll(0x1c701ce06c38fc1c), Ll(0xfdd3fdbb2ee746fd), Ll(0x4d294d52649a1f4d),
            Ll(0x927292e4e0397692), Ll(0x75c9758fbceafa75), Ll(0x061806301e0c3606), Ll(0x8a128a249809ae8a),
            Ll(0xb2f2b2f940794bb2), Ll(0xe6bfe66359d185e6), Ll(0x0e380e70361c7e0e), Ll(0x1f7c1ff8633ee71f),
            Ll(0x62956237f7c45562), Ll(0xd477d4eea3b53ad4), Ll(0xa89aa829324d81a8), Ll(0x966296c4f4315296),
            Ll(0xf9c3f99b3aef62f9), Ll(0xc533c566f697a3c5), Ll(0x25942535b14a1025), Ll(0x597959f220b2ab59),
            Ll(0x842a8454ae15d084), Ll(0x72d572b7a7e4c572), Ll(0x39e439d5dd72ec39), Ll(0x4c2d4c5a6198164c),
            Ll(0x5e655eca3bbc945e), Ll(0x78fd78e785f09f78), Ll(0x38e038ddd870e538), Ll(0x8c0a8c148605988c),
            Ll(0xd163d1c6b2bf17d1), Ll(0xa5aea5410b57e4a5), Ll(0xe2afe2434dd9a1e2), Ll(0x6199612ff8c24e61),
            Ll(0xb3f6b3f1457b42b3), Ll(0x21842115a5423421), Ll(0x9c4a9c94d625089c), Ll(0x1e781ef0663cee1e),
            Ll(0x4311432252866143), Ll(0xc73bc776fc93b1c7), Ll(0xfcd7fcb32be54ffc), Ll(0x0410042014082404),
            Ll(0x515951b208a2e351), Ll(0x995e99bcc72f2599), Ll(0x6da96d4fc4da226d), Ll(0x0d340d68391a650d),
            Ll(0xfacffa8335e979fa), Ll(0xdf5bdfb684a369df), Ll(0x7ee57ed79bfca97e), Ll(0x2490243db4481924),
            Ll(0x3bec3bc5d776fe3b), Ll(0xab96ab313d4b9aab), Ll(0xce1fce3ed181f0ce), Ll(0x1144118855229911),
            Ll(0x8f068f0c8903838f), Ll(0x4e254e4a6b9c044e), Ll(0xb7e6b7d1517366b7), Ll(0xeb8beb0b60cbe0eb),
            Ll(0x3cf03cfdcc78c13c), Ll(0x813e817cbf1ffd81), Ll(0x946a94d4fe354094), Ll(0xf7fbf7eb0cf31cf7),
            Ll(0xb9deb9a1676f18b9), Ll(0x134c13985f268b13), Ll(0x2cb02c7d9c58512c), Ll(0xd36bd3d6b8bb05d3),
            Ll(0xe7bbe76b5cd38ce7), Ll(0x6ea56e57cbdc396e), Ll(0xc437c46ef395aac4), Ll(0x030c03180f061b03),
            Ll(0x5645568a13acdc56), Ll(0x440d441a49885e44), Ll(0x7fe17fdf9efea07f), Ll(0xa99ea921374f88a9),
            Ll(0x2aa82a4d8254672a), Ll(0xbbd6bbb16d6b0abb), Ll(0xc123c146e29f87c1), Ll(0x535153a202a6f153),
            Ll(0xdc57dcae8ba572dc), Ll(0x0b2c0b582716530b), Ll(0x9d4e9d9cd327019d), Ll(0x6cad6c47c1d82b6c),
            Ll(0x31c43195f562a431), Ll(0x74cd7487b9e8f374), Ll(0xf6fff6e309f115f6), Ll(0x4605460a438c4c46),
            Ll(0xac8aac092645a5ac), Ll(0x891e893c970fb589), Ll(0x145014a04428b414), Ll(0xe1a3e15b42dfbae1),
            Ll(0x165816b04e2ca616), Ll(0x3ae83acdd274f73a), Ll(0x69b9696fd0d20669), Ll(0x092409482d124109),
            Ll(0x70dd70a7ade0d770), Ll(0xb6e2b6d954716fb6), Ll(0xd067d0ceb7bd1ed0), Ll(0xed93ed3b7ec7d6ed),
            Ll(0xcc17cc2edb85e2cc), Ll(0x4215422a57846842), Ll(0x985a98b4c22d2c98), Ll(0xa4aaa4490e55eda4),
            Ll(0x28a0285d88507528), Ll(0x5c6d5cda31b8865c), Ll(0xf8c7f8933fed6bf8), Ll(0x86228644a411c286),
        };

        /*
         * The number of rounds of the internal dedicated block cipher.
         */
        /// <summary>
        /// 
        /// </summary>
        const int R = 10;

        /// <summary>
        /// 
        /// </summary>
        static readonly ulong[] Rc = new ulong[R + 1] {
            Ll(0x0000000000000000),
            Ll(0x1823c6e887b8014f),
            Ll(0x36a6d2f5796f9152),
            Ll(0x60bc9b8ea30c7b35),
            Ll(0x1de0d7c22e4bfe57),
            Ll(0x157737e59ff04ada),
            Ll(0x58c9290ab1a06b85),
            Ll(0xbd5d10f4cb3e0567),
            Ll(0xe427418ba77d95d8),
            Ll(0xfbee7c66dd17479e),
            Ll(0xca2dbf07ad5a8333),
        };

        /// <summary>
        /// 
        /// </summary>
        const int Digestbytes = 64;
        /// <summary>
        /// 
        /// </summary>
        const int Digestbits = (8 * Digestbytes); /* 512 */
        /// <summary>
        /// 
        /// </summary>
        const int Wblockbytes = 64;
        /// <summary>
        /// 
        /// </summary>
        const int Wblockbits = (8 * Wblockbytes); /* 512 */
        /// <summary>
        /// 
        /// </summary>
        const int Lengthbytes = 32;
        /// <summary>
        /// 
        /// </summary>
        const int Lengthbits = (8 * Lengthbytes); /* 256 */

        /// <summary>
        /// 
        /// </summary>
        const int LongIteration = 100000000;

        /// <summary>
        /// 
        /// </summary>
        readonly byte[] _bitLength = new byte[Lengthbytes]; /* global number of hashed bits (256-bit counter) */
        /// <summary>
        /// 
        /// </summary>
        readonly byte[] _buffer = new byte[Wblockbytes];     /* buffer of data to hash */
        /// <summary>
        /// 
        /// </summary>
        readonly ulong[] _hash = new ulong[Digestbytes / 8];    /* the hashing state */
        /// <summary>
        /// 
        /// </summary>
        int _bufferBits;                 /* current number of bits on the buffer */
        /// <summary>
        /// 
        /// </summary>
        int _bufferPos;                  /* current (possibly incomplete) byte slot on the buffer */

        /// <summary>
        /// Initialize the hashing state.
        /// </summary>
        void NessiEinit()
        {
            Array.Clear(_hash, 0, _hash.Length);
            Array.Clear(_buffer, 0, _buffer.Length);
            Array.Clear(_bitLength, 0, _bitLength.Length);
            _bufferBits = _bufferPos = 0;
        }

        /// <summary>
        /// The core Whirlpool transform.
        /// </summary>
        void ProcessBuffer()
        {
            int i, r;
            ulong[] k = new ulong[8];        /* the round key */
            ulong[] block = new ulong[8];    /* mu(buffer) */
            ulong[] state = new ulong[8];    /* the cipher state */
            ulong[] l = new ulong[8];
            byte[] buffer = _buffer;
            int bufferOffset = 0;

            /*
             * map the buffer to a block:
             */
            for (i = 0; i < 8; i++, bufferOffset += 8)
            {
                block[i] =
                    (((ulong)buffer[bufferOffset + 0]) << 56) ^
                    (((ulong)buffer[bufferOffset + 1] & 0xffL) << 48) ^
                    (((ulong)buffer[bufferOffset + 2] & 0xffL) << 40) ^
                    (((ulong)buffer[bufferOffset + 3] & 0xffL) << 32) ^
                    (((ulong)buffer[bufferOffset + 4] & 0xffL) << 24) ^
                    (((ulong)buffer[bufferOffset + 5] & 0xffL) << 16) ^
                    (((ulong)buffer[bufferOffset + 6] & 0xffL) << 8) ^
                    (((ulong)buffer[bufferOffset + 7] & 0xffL));
            }
            /*
             * compute and apply K^0 to the cipher state:
             */
            state[0] = block[0] ^ (k[0] = _hash[0]);
            state[1] = block[1] ^ (k[1] = _hash[1]);
            state[2] = block[2] ^ (k[2] = _hash[2]);
            state[3] = block[3] ^ (k[3] = _hash[3]);
            state[4] = block[4] ^ (k[4] = _hash[4]);
            state[5] = block[5] ^ (k[5] = _hash[5]);
            state[6] = block[6] ^ (k[6] = _hash[6]);
            state[7] = block[7] ^ (k[7] = _hash[7]);
            /*
             * iterate over all rounds:
             */
            for (r = 1; r <= R; r++)
            {
                /*
                 * compute K^r from K^{r-1}:
                 */
                l[0] =
                    C0[(int)(k[0] >> 56)] ^
                    C1[(int)(k[7] >> 48) & 0xff] ^
                    C2[(int)(k[6] >> 40) & 0xff] ^
                    C3[(int)(k[5] >> 32) & 0xff] ^
                    C4[(int)(k[4] >> 24) & 0xff] ^
                    C5[(int)(k[3] >> 16) & 0xff] ^
                    C6[(int)(k[2] >> 8) & 0xff] ^
                    C7[(int)(k[1]) & 0xff] ^
                    Rc[r];
                l[1] =
                    C0[(int)(k[1] >> 56)] ^
                    C1[(int)(k[0] >> 48) & 0xff] ^
                    C2[(int)(k[7] >> 40) & 0xff] ^
                    C3[(int)(k[6] >> 32) & 0xff] ^
                    C4[(int)(k[5] >> 24) & 0xff] ^
                    C5[(int)(k[4] >> 16) & 0xff] ^
                    C6[(int)(k[3] >> 8) & 0xff] ^
                    C7[(int)(k[2]) & 0xff];
                l[2] =
                    C0[(int)(k[2] >> 56)] ^
                    C1[(int)(k[1] >> 48) & 0xff] ^
                    C2[(int)(k[0] >> 40) & 0xff] ^
                    C3[(int)(k[7] >> 32) & 0xff] ^
                    C4[(int)(k[6] >> 24) & 0xff] ^
                    C5[(int)(k[5] >> 16) & 0xff] ^
                    C6[(int)(k[4] >> 8) & 0xff] ^
                    C7[(int)(k[3]) & 0xff];
                l[3] =
                    C0[(int)(k[3] >> 56)] ^
                    C1[(int)(k[2] >> 48) & 0xff] ^
                    C2[(int)(k[1] >> 40) & 0xff] ^
                    C3[(int)(k[0] >> 32) & 0xff] ^
                    C4[(int)(k[7] >> 24) & 0xff] ^
                    C5[(int)(k[6] >> 16) & 0xff] ^
                    C6[(int)(k[5] >> 8) & 0xff] ^
                    C7[(int)(k[4]) & 0xff];
                l[4] =
                    C0[(int)(k[4] >> 56)] ^
                    C1[(int)(k[3] >> 48) & 0xff] ^
                    C2[(int)(k[2] >> 40) & 0xff] ^
                    C3[(int)(k[1] >> 32) & 0xff] ^
                    C4[(int)(k[0] >> 24) & 0xff] ^
                    C5[(int)(k[7] >> 16) & 0xff] ^
                    C6[(int)(k[6] >> 8) & 0xff] ^
                    C7[(int)(k[5]) & 0xff];
                l[5] =
                    C0[(int)(k[5] >> 56)] ^
                    C1[(int)(k[4] >> 48) & 0xff] ^
                    C2[(int)(k[3] >> 40) & 0xff] ^
                    C3[(int)(k[2] >> 32) & 0xff] ^
                    C4[(int)(k[1] >> 24) & 0xff] ^
                    C5[(int)(k[0] >> 16) & 0xff] ^
                    C6[(int)(k[7] >> 8) & 0xff] ^
                    C7[(int)(k[6]) & 0xff];
                l[6] =
                    C0[(int)(k[6] >> 56)] ^
                    C1[(int)(k[5] >> 48) & 0xff] ^
                    C2[(int)(k[4] >> 40) & 0xff] ^
                    C3[(int)(k[3] >> 32) & 0xff] ^
                    C4[(int)(k[2] >> 24) & 0xff] ^
                    C5[(int)(k[1] >> 16) & 0xff] ^
                    C6[(int)(k[0] >> 8) & 0xff] ^
                    C7[(int)(k[7]) & 0xff];
                l[7] =
                    C0[(int)(k[7] >> 56)] ^
                    C1[(int)(k[6] >> 48) & 0xff] ^
                    C2[(int)(k[5] >> 40) & 0xff] ^
                    C3[(int)(k[4] >> 32) & 0xff] ^
                    C4[(int)(k[3] >> 24) & 0xff] ^
                    C5[(int)(k[2] >> 16) & 0xff] ^
                    C6[(int)(k[1] >> 8) & 0xff] ^
                    C7[(int)(k[0]) & 0xff];
                k[0] = l[0];
                k[1] = l[1];
                k[2] = l[2];
                k[3] = l[3];
                k[4] = l[4];
                k[5] = l[5];
                k[6] = l[6];
                k[7] = l[7];
                /*
                 * apply the r-th round transformation:
                 */
                l[0] =
                    C0[(int)(state[0] >> 56)] ^
                    C1[(int)(state[7] >> 48) & 0xff] ^
                    C2[(int)(state[6] >> 40) & 0xff] ^
                    C3[(int)(state[5] >> 32) & 0xff] ^
                    C4[(int)(state[4] >> 24) & 0xff] ^
                    C5[(int)(state[3] >> 16) & 0xff] ^
                    C6[(int)(state[2] >> 8) & 0xff] ^
                    C7[(int)(state[1]) & 0xff] ^
                    k[0];
                l[1] =
                    C0[(int)(state[1] >> 56)] ^
                    C1[(int)(state[0] >> 48) & 0xff] ^
                    C2[(int)(state[7] >> 40) & 0xff] ^
                    C3[(int)(state[6] >> 32) & 0xff] ^
                    C4[(int)(state[5] >> 24) & 0xff] ^
                    C5[(int)(state[4] >> 16) & 0xff] ^
                    C6[(int)(state[3] >> 8) & 0xff] ^
                    C7[(int)(state[2]) & 0xff] ^
                    k[1];
                l[2] =
                    C0[(int)(state[2] >> 56)] ^
                    C1[(int)(state[1] >> 48) & 0xff] ^
                    C2[(int)(state[0] >> 40) & 0xff] ^
                    C3[(int)(state[7] >> 32) & 0xff] ^
                    C4[(int)(state[6] >> 24) & 0xff] ^
                    C5[(int)(state[5] >> 16) & 0xff] ^
                    C6[(int)(state[4] >> 8) & 0xff] ^
                    C7[(int)(state[3]) & 0xff] ^
                    k[2];
                l[3] =
                    C0[(int)(state[3] >> 56)] ^
                    C1[(int)(state[2] >> 48) & 0xff] ^
                    C2[(int)(state[1] >> 40) & 0xff] ^
                    C3[(int)(state[0] >> 32) & 0xff] ^
                    C4[(int)(state[7] >> 24) & 0xff] ^
                    C5[(int)(state[6] >> 16) & 0xff] ^
                    C6[(int)(state[5] >> 8) & 0xff] ^
                    C7[(int)(state[4]) & 0xff] ^
                    k[3];
                l[4] =
                    C0[(int)(state[4] >> 56)] ^
                    C1[(int)(state[3] >> 48) & 0xff] ^
                    C2[(int)(state[2] >> 40) & 0xff] ^
                    C3[(int)(state[1] >> 32) & 0xff] ^
                    C4[(int)(state[0] >> 24) & 0xff] ^
                    C5[(int)(state[7] >> 16) & 0xff] ^
                    C6[(int)(state[6] >> 8) & 0xff] ^
                    C7[(int)(state[5]) & 0xff] ^
                    k[4];
                l[5] =
                    C0[(int)(state[5] >> 56)] ^
                    C1[(int)(state[4] >> 48) & 0xff] ^
                    C2[(int)(state[3] >> 40) & 0xff] ^
                    C3[(int)(state[2] >> 32) & 0xff] ^
                    C4[(int)(state[1] >> 24) & 0xff] ^
                    C5[(int)(state[0] >> 16) & 0xff] ^
                    C6[(int)(state[7] >> 8) & 0xff] ^
                    C7[(int)(state[6]) & 0xff] ^
                    k[5];
                l[6] =
                    C0[(int)(state[6] >> 56)] ^
                    C1[(int)(state[5] >> 48) & 0xff] ^
                    C2[(int)(state[4] >> 40) & 0xff] ^
                    C3[(int)(state[3] >> 32) & 0xff] ^
                    C4[(int)(state[2] >> 24) & 0xff] ^
                    C5[(int)(state[1] >> 16) & 0xff] ^
                    C6[(int)(state[0] >> 8) & 0xff] ^
                    C7[(int)(state[7]) & 0xff] ^
                    k[6];
                l[7] =
                    C0[(int)(state[7] >> 56)] ^
                    C1[(int)(state[6] >> 48) & 0xff] ^
                    C2[(int)(state[5] >> 40) & 0xff] ^
                    C3[(int)(state[4] >> 32) & 0xff] ^
                    C4[(int)(state[3] >> 24) & 0xff] ^
                    C5[(int)(state[2] >> 16) & 0xff] ^
                    C6[(int)(state[1] >> 8) & 0xff] ^
                    C7[(int)(state[0]) & 0xff] ^
                    k[7];
                state[0] = l[0];
                state[1] = l[1];
                state[2] = l[2];
                state[3] = l[3];
                state[4] = l[4];
                state[5] = l[5];
                state[6] = l[6];
                state[7] = l[7];
            }
            /*
             * apply the Miyaguchi-Preneel compression function:
             */
            _hash[0] ^= state[0] ^ block[0];
            _hash[1] ^= state[1] ^ block[1];
            _hash[2] ^= state[2] ^ block[2];
            _hash[3] ^= state[3] ^ block[3];
            _hash[4] ^= state[4] ^ block[4];
            _hash[5] ^= state[5] ^ block[5];
            _hash[6] ^= state[6] ^ block[6];
            _hash[7] ^= state[7] ^ block[7];
        }

        /// <summary>
        /// Delivers input data to the hashing algorithm.
        /// @param    source          plaintext data to hash.
        /// @param    sourcePosition  index of leftmost source u8 containing data (1 to 8 bits).
        /// @param    sourceLength    how many bytes of source to process.
        /// This method maintains the invariant: bufferBits &lt; DIGESTBITS
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="sourcePosition">The source position.</param>
        /// <param name="sourceLength">Length of the source.</param>
        void NessiEadd(byte[] source, int sourcePosition, int sourceLength)
        {
            ulong sourceBits = (ulong)sourceLength * 8UL; //input bytes to bits to maintain original source code compatibility
            /*
            sourcePos
            |
            +-------+-------+-------
              ||||||||||||||||||||| source
            +-------+-------+-------
            +-------+-------+-------+-------+-------+-------
            ||||||||||||||||||||||                           buffer
            +-------+-------+-------+-------+-------+-------
                        |
                        bufferPos
            */
            int sourcePos = sourcePosition; /* index of leftmost source u8 containing data (1 to 8 bits). */
            int sourceGap = (8 - ((int)sourceBits & 7)) & 7; /* space on source[sourcePos]. */
            int bufferRem = _bufferBits & 7; /* occupied bits on buffer[bufferPos]. */
            int i;
            uint b, carry;
            byte[] buffer = _buffer;
            byte[] bitLength = _bitLength;
            int bufferBits = _bufferBits;
            int bufferPos = _bufferPos;

            /*
             * tally the count of the added data:
             */
            ulong value = sourceBits;
            for (i = 31, carry = 0; i >= 0 && (carry != 0 || value != Ll(0)); i--)
            {
                carry += bitLength[i] + ((uint)value & 0xff);
                bitLength[i] = (byte)carry;
                carry >>= 8;
                value >>= 8;
            }
            /*
             * process data in chunks of 8 bits (a more efficient approach would be to take whole-word chunks):
             */
            while (sourceBits > 8)
            {
                /* N.B. at least source[sourcePos] and source[sourcePos+1] contain data. */
                /*
                 * take a byte from the source:
                 */
                b = (uint)(((source[sourcePos] << sourceGap) & 0xff) |
                    ((source[sourcePos + 1] & 0xff) >> (8 - sourceGap)));
                /*
                 * process this byte:
                 */
                buffer[bufferPos++] |= (byte)(b >> bufferRem);
                bufferBits += 8 - bufferRem; /* bufferBits = 8*bufferPos; */
                if (bufferBits == Digestbits)
                {
                    /*
                     * process data block:
                     */
                    ProcessBuffer();
                    /*
                     * reset buffer:
                     */
                    bufferBits = bufferPos = 0;
                }
                buffer[bufferPos] = (byte)(b << (8 - bufferRem));
                bufferBits += bufferRem;
                /*
                 * proceed to remaining data:
                 */
                sourceBits -= 8;
                sourcePos++;
            }
            /* now 0 <= sourceBits <= 8;
             * furthermore, all data (if any is left) is in source[sourcePos].
             */
            if (sourceBits > 0)
            {
                b = (uint)((source[sourcePos] << sourceGap) & 0xff); /* bits are left-justified on b. */
                /*
                 * process the remaining bits:
                 */
                buffer[bufferPos] |= (byte)(b >> bufferRem);
            }
            else
            {
                b = 0;
            }
            if ((ulong)bufferRem + sourceBits < 8)
            {
                /*
                 * all remaining data fits on buffer[bufferPos],
                 * and there still remains some space.
                 */
                bufferBits += (int)sourceBits;
            }
            else
            {
                /*
                 * buffer[bufferPos] is full:
                 */
                bufferPos++;
                bufferBits += 8 - bufferRem; /* bufferBits = 8*bufferPos; */
                sourceBits -= (ulong)(8 - bufferRem);
                /* now 0 <= sourceBits < 8;
                 * furthermore, all data (if any is left) is in source[sourcePos].
                 */
                if (bufferBits == Digestbits)
                {
                    /*
                     * process data block:
                     */
                    ProcessBuffer();
                    /*
                     * reset buffer:
                     */
                    bufferBits = bufferPos = 0;
                }
                buffer[bufferPos] = (byte)(b << (8 - bufferRem));
                bufferBits += (int)sourceBits;
            }
            _bufferBits = bufferBits;
            _bufferPos = bufferPos;
        }

        /// <summary>
        /// Get the hash value from the hashing state.
        /// This method uses the invariant: bufferBits &lt; DIGESTBITS
        /// </summary>
        /// <param name="result">The result.</param>
        void NessiEfinalize(byte[] result)
        {
            int i;
            byte[] buffer = _buffer;
            byte[] bitLength = _bitLength;
            int bufferBits = _bufferBits;
            int bufferPos = _bufferPos;
            byte[] digest = result;
            int digestOffset = 0;

            /*
             * append a '1'-bit:
             */
            buffer[bufferPos] |= (byte)(0x80U >> (bufferBits & 7));
            bufferPos++; /* all remaining bits on the current u8 are set to zero. */
            /*
             * pad with zero bits to complete (N*WBLOCKBITS - LENGTHBITS) bits:
             */
            if (bufferPos > Wblockbytes - Lengthbytes)
            {
                if (bufferPos < Wblockbytes)
                {
                    Array.Clear(buffer, bufferPos, Wblockbytes - bufferPos);
                }
                /*
                 * process data block:
                 */
                ProcessBuffer();
                /*
                 * reset buffer:
                 */
                bufferPos = 0;
            }
            if (bufferPos < Wblockbytes - Lengthbytes)
            {
                Array.Clear(buffer, bufferPos, (Wblockbytes - Lengthbytes) - bufferPos);
            }
            bufferPos = Wblockbytes - Lengthbytes;
            /*
             * append bit count of hashed data:
             */
            //memcpy(&buffer[WBLOCKBYTES - LENGTHBYTES], bitLength, LENGTHBYTES);
            Array.Copy(bitLength, 0, buffer, Wblockbytes - Lengthbytes, Lengthbytes);
            /*
             * process data block:
             */
            ProcessBuffer();
            /*
             * return the completed message digest:
             */
            for (i = 0; i < Digestbytes / 8; i++)
            {
                digest[digestOffset + 0] = (byte)(_hash[i] >> 56);
                digest[digestOffset + 1] = (byte)(_hash[i] >> 48);
                digest[digestOffset + 2] = (byte)(_hash[i] >> 40);
                digest[digestOffset + 3] = (byte)(_hash[i] >> 32);
                digest[digestOffset + 4] = (byte)(_hash[i] >> 24);
                digest[digestOffset + 5] = (byte)(_hash[i] >> 16);
                digest[digestOffset + 6] = (byte)(_hash[i] >> 8);
                digest[digestOffset + 7] = (byte)(_hash[i]);
                digestOffset += 8;
            }
            _bufferBits = bufferBits;
            _bufferPos = bufferPos;
        }
    }
}
