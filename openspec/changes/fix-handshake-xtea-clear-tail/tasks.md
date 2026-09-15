## 1. Compatibility correction

- [x] 1.1 Preserve exact XTEA buffer bounds while allowing a revision-742 clear tail.
- [x] 1.2 Keep short sections without a complete XTEA block rejected.

## 2. Regression coverage

- [x] 2.1 Test complete-block decryption with an unchanged clear tail.
- [x] 2.2 Test clear-tail acceptance through both lobby and world decoders.

## 3. Verification

- [ ] 3.1 Run focused handshake tests and the complete GameWorld test project.
- [ ] 3.2 Run the affected build, strict OpenSpec validation, and manual revision-742 login.
