using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using Hagalaz.Cache.Abstractions.Model;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Extensions;

namespace Hagalaz.Cache.Types
{
    /// <summary>
    /// Represend's owner item definition.
    /// </summary>
    public class ItemType : IItemType
    {
        /// <summary>
        /// Contains item extra data.
        /// </summary>
        /// <value>The extra data.</value>
        public IReadOnlyDictionary<int, object>? ExtraData { get; private set; }
        /// <summary>
        /// Contains ID of this item.
        /// </summary>
        public int Id { get; }
        /// <summary>
        /// Gets the interface model id.
        /// </summary>
        /// <value>The interface model id.</value>
        public int InterfaceModelId { get; private set; }
        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; private set; }
        /// <summary>
        /// Gets the model zoom.
        /// </summary>
        /// <value>The model zoom.</value>
        public int ModelZoom { get; private set; }
        /// <summary>
        /// Gets the model rotation1.
        /// </summary>
        /// <value>The model rotation1.</value>
        public int ModelRotation1 { get; private set; }
        /// <summary>
        /// Gets the model rotation2.
        /// </summary>
        /// <value>The model rotation2.</value>
        public int ModelRotation2 { get; private set; }
        /// <summary>
        /// Gets the model offset1.
        /// </summary>
        /// <value>The model offset1.</value>
        public int ModelOffset1 { get; private set; }
        /// <summary>
        /// Gets the model offset2.
        /// </summary>
        /// <value>The model offset2.</value>
        public int ModelOffset2 { get; private set; }
        /// <summary>
        /// Gets the stackable.
        /// </summary>
        /// <value>The stackable.</value>
        public int StackableType { get; private set; }
        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <value>The value.</value>
        public int Value { get; private set; }
        /// <summary>
        /// Gets a value indicating whether [members only].
        /// </summary>
        /// <value><c>true</c> if [members only]; otherwise, <c>false</c>.</value>
        public bool MembersOnly { get; private set; }
        /// <summary>
        /// Gets the male worn model id1.
        /// </summary>
        /// <value>The male worn model id1.</value>
        public int MaleWornModelId1 { get; private set; }
        /// <summary>
        /// Gets the female worn model id1.
        /// </summary>
        /// <value>The female worn model id1.</value>
        public int MaleWornModelId2 { get; private set; }
        /// <summary>
        /// Gets the male worn model id2.
        /// </summary>
        /// <value>The male worn model id2.</value>
        public int FemaleWornModelId1 { get; private set; }
        /// <summary>
        /// Gets the female worn model id2.
        /// </summary>
        /// <value>The female worn model id2.</value>
        public int FemaleWornModelId2 { get; private set; }
        /// <summary>
        /// Gets the ground options.
        /// </summary>
        /// <value>The ground options.</value>
        public string?[] GroundOptions { get; private set; }
        /// <summary>
        /// Gets the inventory options.
        /// </summary>
        /// <value>The inventory options.</value>
        public string?[] InventoryOptions { get; private set; }
        /// <summary>
        /// Gets the original model colors.
        /// </summary>
        /// <value>The original model colors.</value>
        public int[]? OriginalModelColors { get; private set; }
        /// <summary>
        /// Gets the modified model colors.
        /// </summary>
        /// <value>The modified model colors.</value>
        public int[]? ModifiedModelColors { get; private set; }
        /// <summary>
        /// Gets the texture colour1.
        /// </summary>
        /// <value>The texture colour1.</value>
        public int[]? OriginalTextureColors { get; private set; }
        /// <summary>
        /// Gets the texture colour2.
        /// </summary>
        /// <value>The texture colour2.</value>
        public int[]? ModifiedTextureColors { get; private set; }
        /// <summary>
        /// Gets the unknown array1.
        /// </summary>
        /// <value>The unknown array1.</value>
        public sbyte[]? UnknownArray1 { get; private set; }
        /// <summary>
        /// Gets the unknown array2.
        /// </summary>
        /// <value>The unknown array2.</value>
        public int[]? QuestIDs { get; private set; }
        /// <summary>
        /// Gets a value indicating whether this <see cref="ItemType" /> is unnoted.
        /// </summary>
        /// <value><c>true</c> if unnoted; otherwise, <c>false</c>.</value>
        public bool Unnoted { get; private set; }
        /// <summary>
        /// Gets the colour equip1.
        /// </summary>
        /// <value>The colour equip1.</value>
        public int MaleWornModelId3 { get; private set; }
        /// <summary>
        /// Gets the colour equip2.
        /// </summary>
        /// <value>The colour equip2.</value>
        public int FemaleWornModelId3 { get; private set; }
        /// <summary>
        /// Gets the unknown int0.
        /// </summary>
        /// <value>The unknown int0.</value>
        public int MultiStackSize { get; private set; }
        /// <summary>
        /// Gets the unknown int1.
        /// </summary>
        /// <value>The unknown int1.</value>
        public int MaleHeadModel { get; private set; }
        /// <summary>
        /// Gets the unknown int2.
        /// </summary>
        /// <value>The unknown int2.</value>
        public int FemaleHeadModel { get; private set; }
        /// <summary>
        /// Gets the unknown int3.
        /// </summary>
        /// <value>The unknown int3.</value>
        public int MaleHeadModel2 { get; private set; }
        /// <summary>
        /// Gets the unknown int4.
        /// </summary>
        /// <value>The unknown int4.</value>
        public int FemaleHeadModel2 { get; private set; }
        /// <summary>
        /// Gets the unknown int5.
        /// </summary>
        /// <value>The unknown int5.</value>
        public int Zan2D { get; private set; }
        /// <summary>
        /// Gets the unknown int6.
        /// </summary>
        /// <value>The unknown int6.</value>
        public int UnknownInt6 { get; private set; }
        /// <summary>
        /// Gets the note ID.
        /// </summary>
        /// <value>The note ID.</value>
        public int NoteId { get; private set; }
        /// <summary>
        /// Gets the note template ID.
        /// </summary>
        /// <value>The note template ID.</value>
        public int NoteTemplateId { get; private set; }
        /// <summary>
        /// Gets the stack ids.
        /// </summary>
        /// <value>The stack ids.</value>
        public int[]? StackIds { get; private set; }
        /// <summary>
        /// Gets the stack amounts.
        /// </summary>
        /// <value>The stack amounts.</value>
        public int[]? StackAmounts { get; private set; }
        /// <summary>
        /// Gets the unknown int7.
        /// </summary>
        /// <value>The unknown int7.</value>
        public int ScaleX { get; private set; }
        /// <summary>
        /// Gets the unknown int8.
        /// </summary>
        /// <value>The unknown int8.</value>
        public int ScaleY { get; private set; }
        /// <summary>
        /// Gets the unknown int9.
        /// </summary>
        /// <value>The unknown int9.</value>
        public int ScaleZ { get; private set; }
        /// <summary>
        /// Gets the unknown int10.
        /// </summary>
        /// <value>The unknown int10.</value>
        public int Ambient { get; private set; }
        /// <summary>
        /// Gets the unknown int11.
        /// </summary>
        /// <value>The unknown int11.</value>
        public int Contrast { get; private set; }
        /// <summary>
        /// Gets the team id.
        /// </summary>
        /// <value>The team id.</value>
        public int TeamId { get; private set; }
        /// <summary>
        /// Gets the lend ID.
        /// </summary>
        /// <value>The lend ID.</value>
        public int LendId { get; private set; }
        /// <summary>
        /// Gets the lend template ID.
        /// </summary>
        /// <value>The lend template ID.</value>
        public int LendTemplateId { get; private set; }
        /// <summary>
        /// Gets the unknown int12.
        /// </summary>
        /// <value>The unknown int12.</value>
        public int MaleWearXOffset { get; private set; }
        /// <summary>
        /// Gets the unknown int13.
        /// </summary>
        /// <value>The unknown int13.</value>
        public int MaleWearYOffset { get; private set; }
        /// <summary>
        /// Gets the unknown int14.
        /// </summary>
        /// <value>The unknown int14.</value>
        public int MaleWearZOffset { get; private set; }
        /// <summary>
        /// Gets the unknown int15.
        /// </summary>
        /// <value>The unknown int15.</value>
        public int FemaleWearXOffset { get; private set; }
        /// <summary>
        /// Gets the unknown int16.
        /// </summary>
        /// <value>The unknown int16.</value>
        public int FemaleWearYOffset { get; private set; }
        /// <summary>
        /// Gets the unknown int17.
        /// </summary>
        /// <value>The unknown int17.</value>
        public int FemaleWearZOffset { get; private set; }
        /// <summary>
        /// Gets the unknown int18.
        /// </summary>
        /// <value>The unknown int18.</value>
        public int UnknownInt18 { get; private set; }
        /// <summary>
        /// Gets the unknown int19.
        /// </summary>
        /// <value>The unknown int19.</value>
        public int UnknownInt19 { get; private set; }
        /// <summary>
        /// Gets the unknown int20.
        /// </summary>
        /// <value>The unknown int20.</value>
        public int UnknownInt20 { get; private set; }
        /// <summary>
        /// Gets the unknown int21.
        /// </summary>
        /// <value>The unknown int21.</value>
        public int UnknownInt21 { get; private set; }
        /// <summary>
        /// Gets the unknown int22.
        /// </summary>
        /// <value>The unknown int22.</value>
        public int UnknownInt22 { get; private set; }
        /// <summary>
        /// Gets the unknown int23.
        /// </summary>
        /// <value>The unknown int23.</value>
        public int UnknownInt23 { get; private set; }
        /// <summary>
        /// Gets the unknown int24.
        /// </summary>
        /// <value>The unknown int24.</value>
        public int UnknownInt24 { get; private set; }
        /// <summary>
        /// Gets the unknown int25.
        /// </summary>
        /// <value>The unknown int25.</value>
        public int UnknownInt25 { get; private set; }
        /// <summary>
        /// Gets the unknown int26.
        /// </summary>
        /// <value>The unknown int26.</value>
        public int PickSizeShift { get; private set; }
        /// <summary>
        /// Gets the unknown item ID.
        /// </summary>
        /// <value>The unknown item ID.</value>
        public int BoughtItemId { get; private set; }
        /// <summary>
        /// Gets the unknown template ID.
        /// </summary>
        /// <value>The unknown template ID.</value>
        public int BoughtTemplateId { get; private set; }
        /// <summary>
        /// Gets the equip ID.
        /// </summary>
        /// <value>
        /// The equip ID.
        /// </value>
        public sbyte EquipSlot { get; private set; }
        /// <summary>
        /// Gets the type of the equip.
        /// </summary>
        /// <value>
        /// The type of the equip.
        /// </value>
        public sbyte EquipType { get; private set; }
        /// <summary>
        /// Gets some equip int.
        /// </summary>
        /// <value>
        /// Some equip int.
        /// </value>
        public sbyte SomeEquipInt { get; private set; }
        /// <summary>
        /// Whether the item is noted.
        /// </summary>
        public bool Noted => NoteId != -1 && NoteTemplateId != -1;
        /// <summary>
        /// Gets a value indicating whether this is stackable.
        /// </summary>
        /// <value>
        ///   <c>true</c> if stackable; otherwise, <c>false</c>.
        /// </value>
        public bool Stackable => StackableType == 1 || Noted;
        /// <summary>
        /// Contains boolean wheter item is wearable.
        /// </summary>
        public bool HasWearModel => MaleWornModelId1 >= 0 || FemaleWornModelId1 >= 0;
        /// <summary>
        /// Gets the type of the item degrade (if dropped).
        /// Used to determine if the item should be protected, destroyed or dropped.
        /// </summary>
        public DegradeType DegradeType
        {
            get
            {
                if (ExtraData != null)
                {
                    if (!ExtraData.ContainsKey(1397))
                        return DegradeType.DropItem;
                    return (DegradeType)ExtraData[1397];
                }
                return DegradeType.DropItem;
            }
        }

        /// <summary>
        /// Get's item render animation ID.
        /// </summary>
        /// <returns>Return's render animation ID or -1 if it doesn't have it.</returns>
        public int RenderAnimationId
        {
            get
            {
                if (ExtraData != null)
                {
                    if (!ExtraData.ContainsKey(644))
                        return -1;
                    if (ExtraData[644] is int @int)
                        return @int;
                }
                return -1;
            }
        }

        /// <summary>
        /// Determines whether [has destroy option].
        /// </summary>
        /// <returns><c>true</c> if [has destroy option]; otherwise, <c>false</c>.</returns>
        public bool HasDestroyOption
        {
            get
            {
                foreach (var option in InventoryOptions)
                {
                    if (option != null)
                        if (option.Equals("Destroy"))
                            return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Construct's new item definition with given ID.
        /// </summary>
        /// <param name="id">ID of the item.</param>
        public ItemType(int id)
        {
            Id = id;
            GroundOptions = new string?[] { null, null, "take", null, null };
            InventoryOptions = new string?[] { null, null, null, null, "drop" };
            Name = "null";
            MaleWornModelId1 = -1;
            FemaleWornModelId1 = -1;
            MaleWornModelId2 = -1;
            FemaleWornModelId2 = -1;
            ModelZoom = 2000;
            LendId = -1;
            LendTemplateId = -1;
            NoteId = -1;
            NoteTemplateId = -1;
            BoughtItemId = -1;
            BoughtTemplateId = -1;
            ScaleZ = 128;
            Value = 1;
            MaleWornModelId3 = -1;
            FemaleWornModelId3 = -1;
            EquipSlot = -1;
            EquipType = -1;
        }

        /// <summary>
        /// Parse's itemdefinition opcode.
        /// </summary>
        /// <param name="stream">Buffer from which additional data will be readed.</param>
        /// <exception cref="Exception">Unknown opcode:" + opcode</exception>
        public void Decode(MemoryStream stream)
        {
            while (true)
            {
                int opcode = stream.ReadUnsignedByte();
                if (opcode == 0)
                {
                    return;
                }
                if (opcode == 1)
                    InterfaceModelId = stream.ReadBigSmart();
                else if (opcode == 2)
                    Name = stream.ReadString();
                else if (opcode == 4)
                    ModelZoom = stream.ReadUnsignedShort();
                else if (opcode == 5)
                    ModelRotation1 = stream.ReadUnsignedShort();
                else if (opcode == 6)
                    ModelRotation2 = stream.ReadUnsignedShort();
                else if (opcode == 7)
                {
                    ModelOffset1 = stream.ReadUnsignedShort();
                    if (ModelOffset1 > 32767)
                        ModelOffset1 -= 65536; //modelOffset1 <<= 0;
                }
                else if (opcode == 8)
                {
                    ModelOffset2 = stream.ReadUnsignedShort();
                    if (ModelOffset2 > 32767)
                        ModelOffset2 -= 65536; //modelOffset2 <<= 0;
                }
                else if (opcode == 11)
                    StackableType = 1;
                else if (opcode == 12)
                    Value = stream.ReadInt();
                else if (opcode == 13)
                    EquipSlot = (sbyte)stream.ReadUnsignedByte();
                else if (opcode == 14)
                    EquipType = (sbyte)stream.ReadUnsignedByte();
                else if (opcode == 16)
                    MembersOnly = true;
                else if (opcode == 18)
                    MultiStackSize = stream.ReadUnsignedShort();
                else if (opcode == 23)
                    MaleWornModelId1 = stream.ReadBigSmart();
                else if (opcode == 24)
                    MaleWornModelId2 = stream.ReadBigSmart();
                else if (opcode == 25)
                    FemaleWornModelId1 = stream.ReadBigSmart();
                else if (opcode == 26)
                    FemaleWornModelId2 = stream.ReadBigSmart();
                else if (opcode == 27)
                    SomeEquipInt = (sbyte)stream.ReadUnsignedByte();
                else if (opcode >= 30 && opcode < 35)
                    GroundOptions[opcode - 30] = stream.ReadString();
                else if (opcode >= 35 && opcode < 40)
                    InventoryOptions[opcode - 35] = stream.ReadString();
                else if (opcode == 40)
                {
                    int length = stream.ReadUnsignedByte();
                    OriginalModelColors = new int[length];
                    ModifiedModelColors = new int[length];
                    for (int index = 0; index < length; index++)
                    {
                        OriginalModelColors[index] = stream.ReadUnsignedShort();
                        ModifiedModelColors[index] = stream.ReadUnsignedShort();
                    }
                }
                else if (opcode == 41)
                {
                    int length = stream.ReadUnsignedByte();
                    OriginalTextureColors = new int[length];
                    ModifiedTextureColors = new int[length];
                    for (int index = 0; index < length; index++)
                    {
                        OriginalTextureColors[index] = stream.ReadUnsignedShort();
                        ModifiedTextureColors[index] = stream.ReadUnsignedShort();
                    }
                }
                else if (opcode == 42)
                {
                    int length = stream.ReadUnsignedByte();
                    UnknownArray1 = new sbyte[length];
                    for (int index = 0; index < length; index++)
                        UnknownArray1[index] = (sbyte)stream.ReadSignedByte();
                }
                else if (opcode == 43)
                {
                    // tooltip color
                    int someInt = stream.ReadInt();
                    //somebool is true
                }
                else if (opcode == 44)
                {
                    int i27 = stream.ReadUnsignedShort();
                    /*int i_28_ = 0;
                    for (int i_29_ = i_27_; i_29_ > 0; i_29_ >>= 1)
                        i_28_++;
                    sbyte[] aByteArray5278 = new sbyte[i_28_];
                    sbyte i_30_ = 0;
                    for (int i_31_ = 0; i_31_ < i_28_; i_31_++)
                    {
                        if ((i_27_ & 1 << i_31_) > 0)
                        {
                            aByteArray5278[i_31_] = i_30_;
                            i_30_++;
                        }
                        else
                            aByteArray5278[i_31_] = (sbyte)-1;
                    }*/
                }
                else if (opcode == 45)
                {
                    int i32 = stream.ReadUnsignedShort();
                    /*int i_33_ = 0;
                    for (int i_34_ = i_32_; i_34_ > 0; i_34_ >>= 1)
                        i_33_++;
                    sbyte[] aByteArray5299 = new sbyte[i_33_];
                    sbyte i_35_ = 0;
                    for (int i_36_ = 0; i_36_ < i_33_; i_36_++)
                    {
                        if ((i_32_ & 1 << i_36_) > 0)
                        {
                            aByteArray5299[i_36_] = i_35_;
                            i_35_++;
                        }
                        else
                            aByteArray5299[i_36_] = (sbyte)-1;
                    }*/
                }
                else if (opcode == 65)
                    Unnoted = true;
                else if (opcode == 78)
                    MaleWornModelId3 = stream.ReadBigSmart();
                else if (opcode == 79)
                    FemaleWornModelId3 = stream.ReadBigSmart();
                else if (opcode == 90)
                    MaleHeadModel = stream.ReadBigSmart();
                else if (opcode == 91)
                    FemaleHeadModel = stream.ReadBigSmart();
                else if (opcode == 92)
                    MaleHeadModel2 = stream.ReadBigSmart();
                else if (opcode == 93)
                    FemaleHeadModel2 = stream.ReadBigSmart();
                else if (opcode == 95)
                    Zan2D = stream.ReadUnsignedShort();
                else if (opcode == 96)
                    UnknownInt6 = stream.ReadUnsignedByte();
                else if (opcode == 97)
                    NoteId = stream.ReadUnsignedShort();
                else if (opcode == 98)
                    NoteTemplateId = stream.ReadUnsignedShort();
                else if (opcode >= 100 && opcode < 110)
                {
                    if (StackIds == null || StackAmounts == null)
                    {
                        StackIds = new int[10];
                        StackAmounts = new int[10];
                    }

                    StackIds[opcode - 100] = stream.ReadUnsignedShort();
                    StackAmounts[opcode - 100] = stream.ReadUnsignedShort();
                }
                else if (opcode == 110)
                    ScaleX = stream.ReadUnsignedShort();
                else if (opcode == 111)
                    ScaleY = stream.ReadUnsignedShort();
                else if (opcode == 112)
                    ScaleZ = stream.ReadUnsignedShort();
                else if (opcode == 113)
                    Ambient = (sbyte)stream.ReadSignedByte();
                else if (opcode == 114)
                    Contrast = (sbyte)stream.ReadSignedByte() * 5;
                else if (opcode == 115)
                    TeamId = (byte)stream.ReadUnsignedByte();
                else if (opcode == 121)
                    LendId = stream.ReadUnsignedShort();
                else if (opcode == 122)
                    LendTemplateId = stream.ReadUnsignedShort();
                else if (opcode == 125)
                {
                    MaleWearXOffset = (sbyte)stream.ReadSignedByte() << 2;
                    MaleWearYOffset = (sbyte)stream.ReadSignedByte() << 2;
                    MaleWearZOffset = (sbyte)stream.ReadSignedByte() << 2;
                }
                else if (opcode == 126)
                {
                    FemaleWearXOffset = (sbyte)stream.ReadSignedByte() << 2;
                    FemaleWearYOffset = (sbyte)stream.ReadSignedByte() << 2;
                    FemaleWearZOffset = (sbyte)stream.ReadSignedByte() << 2;
                }
                else if (opcode == 127)
                {
                    UnknownInt18 = (byte)stream.ReadUnsignedByte();
                    UnknownInt19 = stream.ReadUnsignedShort();
                }
                else if (opcode == 128)
                {
                    UnknownInt20 = (byte)stream.ReadUnsignedByte();
                    UnknownInt21 = stream.ReadUnsignedShort();
                }
                else if (opcode == 129)
                {
                    UnknownInt22 = (byte)stream.ReadUnsignedByte();
                    UnknownInt23 = stream.ReadUnsignedShort();
                }
                else if (opcode == 130)
                {
                    UnknownInt24 = (byte)stream.ReadUnsignedByte();
                    UnknownInt25 = stream.ReadUnsignedShort();
                }
                else if (opcode == 132)
                {
                    int length = stream.ReadUnsignedByte();
                    QuestIDs = new int[length];
                    for (int index = 0; index < length; index++)
                        QuestIDs[index] = stream.ReadUnsignedShort();
                }
                else if (opcode == 134)
                    PickSizeShift = stream.ReadUnsignedByte();
                else if (opcode == 139)
                    BoughtItemId = stream.ReadUnsignedShort();
                else if (opcode == 140)
                    BoughtTemplateId = stream.ReadUnsignedShort();
                else if (opcode >= 142 && opcode < 147)
                {
                    stream.ReadUnsignedShort();
                }
                else if (opcode >= 150 && opcode < 155)
                {
                    stream.ReadUnsignedShort();
                }
                else if (opcode == 242)
                {
                    int oldInvModel = stream.ReadBigSmart();
                    int oldInvZoom = stream.ReadBigSmart();
                }
                else if (opcode == 243)
                {
                    int oldMaleEquip3 = stream.ReadBigSmart();
                }
                else if (opcode == 244)
                {
                    int oldFemaleEquip3 = stream.ReadBigSmart();
                }
                else if (opcode == 245)
                {
                    int oldMaleEquip2 = stream.ReadBigSmart();
                }
                else if (opcode == 246)
                {
                    int oldFemaleEquip2 = stream.ReadBigSmart();
                }
                else if (opcode == 247)
                {
                    int oldMaleEquip1 = stream.ReadBigSmart();
                }
                else if (opcode == 248)
                {
                    int oldFemaleEquip1 = stream.ReadBigSmart();
                }
                else if (opcode == 249)
                {
                    var length = stream.ReadUnsignedByte();
                    var extraData = new Dictionary<int, object>(length);
                    for (var index = 0; index < length; index++)
                    {
                        var stringInstance = stream.ReadUnsignedByte() == 1;
                        var key = stream.ReadMedInt();
                        object val;
                        if (stringInstance)
                            val = stream.ReadString();
                        else
                            val = stream.ReadInt();
                        extraData.Remove(key);
                        extraData.Add(key, val);
                    }
                    ExtraData = extraData;
                }
                else if (opcode == 250)
                {
                    int oldEquipType = stream.ReadUnsignedByte();
                }
                else if (opcode == 251)
                {
                    int length = stream.ReadUnsignedByte();
                    short[] oldModelColors = new short[length];
                    short[] oldModifiedModelColors = new short[length];
                    for (int i = 0; i < length; i++)
                    {
                        oldModelColors[i] = (short)stream.ReadUnsignedShort();
                        oldModifiedModelColors[i] = (short)stream.ReadUnsignedShort();
                    }
                }
                else if (opcode == 252)
                {
                    int length = stream.ReadUnsignedByte();
                    short[] oldModelTextures = new short[length];
                    short[] oldModifiedModelTextures = new short[length];
                    for (int i = 0; i < length; i++)
                    {
                        oldModelTextures[i] = (short)stream.ReadUnsignedShort();
                        oldModifiedModelTextures[i] = (short)stream.ReadUnsignedShort();
                    }
                }
                else if (opcode == 253)
                {
                    int oldModelRotation1 = stream.ReadUnsignedShort();
                    int oldModelRotation2 = stream.ReadUnsignedShort();
                    int oldModelOffset1 = stream.ReadUnsignedShort();
                    int oldModelOffset2 = stream.ReadUnsignedShort();
                }
                else
                {
                    throw new Exception("Unknown opcode:" + opcode);
                }
            }
        }

        /// <summary>
        /// Gets the data.
        /// </summary>
        /// <returns></returns>
        public MemoryStream Encode()
        {
            var writer = new MemoryStream();
            writer.WriteByte(1);
            writer.WriteBigSmart(InterfaceModelId);

            if (!Name.Equals("null") && NoteTemplateId == -1)
            {
                writer.WriteByte(2);
                writer.WriteString(Name);
            }

            if (ModelZoom != 2000)
            {
                writer.WriteByte(4);
                writer.WriteShort(ModelZoom);
            }

            if (ModelRotation1 != 0)
            {
                writer.WriteByte(5);
                writer.WriteShort(ModelRotation1);
            }

            if (ModelRotation2 != 0)
            {
                writer.WriteByte(6);
                writer.WriteShort(ModelRotation2);
            }

            if (ModelOffset1 != 0)
            {
                writer.WriteByte(7);
                int value = ModelOffset1 >>= 0;
                if (value < 0)
                    value += ushort.MaxValue;
                writer.WriteShort(value);
            }

            if (ModelOffset2 != 0)
            {
                writer.WriteByte(8);
                int value = ModelOffset2 >>= 0;
                if (value < 0)
                    value += ushort.MaxValue;
                writer.WriteShort(value);
            }

            if (StackableType >= 1 && NoteTemplateId == -1)
            {
                writer.WriteByte(11);
            }

            if (Value != 1 && LendTemplateId == -1)
            {
                writer.WriteByte(12);
                writer.WriteInt(Value);
            }

            if (EquipSlot != -1)
            {
                writer.WriteByte(13);
                writer.WriteByte(EquipSlot);
            }

            if (EquipType != -1)
            {
                writer.WriteByte(14);
                writer.WriteByte(EquipType);
            }

            if (MembersOnly && NoteId == -1)
            {
                writer.WriteByte(16);
            }

            if (MaleWornModelId1 != -1)
            {
                writer.WriteByte(23);
                writer.WriteBigSmart(MaleWornModelId1);
            }
            if (MaleWornModelId2 != -1)
            {
                writer.WriteByte(24);
                writer.WriteBigSmart(MaleWornModelId2);
            }

            if (FemaleWornModelId1 != -1)
            {
                writer.WriteByte(25);
                writer.WriteBigSmart(FemaleWornModelId1);
            }

            if (FemaleWornModelId2 != -1)
            {
                writer.WriteByte(26);
                writer.WriteBigSmart(FemaleWornModelId2);
            }

            // TODO 

            if (Unnoted)
            {
                writer.WriteByte(65);
            }

            if (MaleWornModelId3 != -1)
            {
                writer.WriteByte(78);
                writer.WriteBigSmart(MaleWornModelId3);
            }

            if (FemaleWornModelId3 != -1)
            {
                writer.WriteByte(79);
                writer.WriteBigSmart(FemaleWornModelId3);
            }

            if (NoteId != -1)
            {
                writer.WriteByte(97);
                writer.WriteShort(NoteId);
            }

            if (NoteTemplateId != -1)
            {
                writer.WriteByte(98);
                writer.WriteShort(NoteTemplateId);
            }

            if (TeamId != 0)
            {
                writer.WriteByte(115);
                writer.WriteByte(TeamId);
            }

            if (LendId != -1)
            {
                writer.WriteByte(121);
                writer.WriteShort(LendId);
            }

            if (LendTemplateId != -1)
            {
                writer.WriteByte(122);
                writer.WriteShort(LendTemplateId);
            }

            if (ExtraData != null && ExtraData.Count > 0)
            {
                writer.WriteByte(249);
                writer.WriteByte(ExtraData.Count);
                foreach (var pair in ExtraData)
                {
                    var data = pair.Value;
                    writer.WriteByte(data is string ? 1 : 0);
                    writer.WriteMedInt(pair.Key);
                    if (data is string)
                    {
                        writer.WriteString((string)data);
                    }
                    else
                    {
                        writer.WriteInt((int)data);
                    }
                }
            }
            return writer;
        }

        /// <summary>
        /// Add's note data to this item definition.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="template">The template.</param>
        public void MakeNote(IItemType item, IItemType template)
        {
            MembersOnly = item.MembersOnly;
            InterfaceModelId = template.InterfaceModelId;
            OriginalModelColors = template.OriginalModelColors;
            Name = item.Name;
            ModelOffset2 = template.ModelOffset2;
            OriginalTextureColors = template.OriginalTextureColors;
            Value = item.Value;
            ModelRotation2 = template.ModelRotation2;
            StackableType = 1;
            ModifiedModelColors = template.ModifiedModelColors;
            ModelRotation1 = template.ModelRotation1;
            ModelZoom = template.ModelZoom;
            OriginalTextureColors = template.OriginalTextureColors;
            Zan2D = template.Zan2D;
        }

        /// <summary>
        /// Add's lend data to this item definition.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="template">The template.</param>
        public void MakeLend(IItemType item, IItemType template)
        {
            MaleWornModelId2 = item.MaleWornModelId2;
            MaleHeadModel = item.MaleHeadModel;
            FemaleWornModelId1 = item.FemaleWornModelId1;
            MaleWearYOffset = item.MaleWearYOffset;
            MembersOnly = item.MembersOnly;
            InterfaceModelId = template.InterfaceModelId;
            ModifiedTextureColors = item.ModifiedTextureColors;
            FemaleWearXOffset = item.FemaleWearXOffset;
            GroundOptions = item.GroundOptions;
            UnknownArray1 = item.UnknownArray1;
            ModelRotation1 = template.ModelRotation1;
            ModelRotation2 = template.ModelRotation2;
            OriginalModelColors = item.OriginalModelColors;
            Name = item.Name;
            FemaleHeadModel2 = item.FemaleHeadModel2;
            MaleWornModelId1 = item.MaleWornModelId1;
            MaleWearZOffset = item.MaleWearZOffset;
            MaleHeadModel2 = item.MaleHeadModel2;
            MaleWornModelId3 = item.MaleWornModelId3;
            TeamId = item.TeamId;
            ModelOffset2 = template.ModelOffset2;
            ExtraData = item.ExtraData;
            ModifiedModelColors = item.ModifiedModelColors;
            MaleWearXOffset = item.MaleWearXOffset;
            FemaleWornModelId3 = item.FemaleWornModelId3;
            FemaleHeadModel = item.FemaleHeadModel;
            ModelOffset1 = template.ModelOffset1;
            FemaleWearZOffset = item.FemaleWearZOffset;
            OriginalTextureColors = item.OriginalTextureColors;
            Value = 0;
            Zan2D = template.Zan2D;
            ModelZoom = template.ModelZoom;
            FemaleWearYOffset = item.FemaleWearYOffset;
            InventoryOptions = new string[5];
            FemaleWornModelId2 = item.FemaleWornModelId2;
            if (item.InventoryOptions != null)
                for (int i = 0; i < 4; i++)
                    InventoryOptions[i] = item.InventoryOptions[i];
            InventoryOptions[4] = "Discard";
            EquipSlot = item.EquipSlot;
            EquipType = item.EquipType;
        }

        /// <summary>
        /// Add's data which is unknown to this item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="template">The template.</param>
        public void MakeBought(IItemType item, IItemType template)
        {
            FemaleWornModelId2 = item.FemaleWornModelId2;
            FemaleWearXOffset = item.FemaleWearXOffset;
            InventoryOptions = new string[5];
            ModelRotation2 = template.ModelRotation2;
            Name = item.Name;
            MaleWornModelId1 = item.MaleWornModelId1;
            ModelOffset2 = template.ModelOffset2;
            MaleWearXOffset = item.MaleWearXOffset;
            MaleWornModelId2 = item.MaleWornModelId2;
            FemaleWornModelId1 = item.FemaleWornModelId1;
            MaleHeadModel = item.MaleHeadModel;
            Zan2D = template.Zan2D;
            ModelOffset1 = template.ModelOffset1;
            UnknownArray1 = item.UnknownArray1;
            StackableType = item.StackableType;
            ModelRotation1 = template.ModelRotation1;
            OriginalTextureColors = item.OriginalTextureColors;
            MaleHeadModel2 = item.MaleHeadModel2;
            FemaleHeadModel2 = item.FemaleHeadModel2;
            MaleWornModelId3 = item.MaleWornModelId3;
            ModifiedTextureColors = item.ModifiedTextureColors;
            FemaleWearZOffset = item.FemaleWearZOffset;
            ModifiedModelColors = item.ModifiedModelColors;
            ModelZoom = template.ModelZoom;
            FemaleWornModelId3 = item.FemaleWornModelId3;
            TeamId = item.TeamId;
            Value = 0;
            GroundOptions = item.GroundOptions;
            OriginalModelColors = item.OriginalModelColors;
            MaleWearYOffset = item.MaleWearYOffset;
            MembersOnly = item.MembersOnly;
            FemaleWearYOffset = item.FemaleWearYOffset;
            ExtraData = item.ExtraData;
            MaleWearZOffset = item.MaleWearZOffset;
            InterfaceModelId = template.InterfaceModelId;
            FemaleHeadModel = item.FemaleHeadModel;
            if (item.InventoryOptions != null)
            {
                for (int i = 0; i < 4; i++)
                    InventoryOptions[i] = item.InventoryOptions[i];
            }
            InventoryOptions[4] = "Discard";
            EquipSlot = item.EquipSlot;
            EquipType = item.EquipType;
        }

        /// <summary>
        /// Get's if this item has a special bar.
        /// </summary>
        /// <returns>Return's true if this item has special bar.</returns>
        public bool HasSpecialBar()
        {
            if (ExtraData != null)
            {
                if (!ExtraData.ContainsKey(687))
                    return false;
                if (((int)ExtraData[687]) == 1)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Get's Quest ID of this item.
        /// </summary>
        /// <returns>Return's quest ID or -1 if it's not found.</returns>
        public int GetQuestId()
        {
            if (ExtraData != null)
            {
                if (!ExtraData.ContainsKey(861))
                    return -1;
                if (ExtraData[861] is int)
                    return (int)ExtraData[861];
            }
            return -1;
        }

        /// <summary>
        /// Gets the attack speed.
        /// </summary>
        /// <returns></returns>
        public int GetAttackSpeed()
        {
            if (ExtraData != null)
            {
                if (ExtraData.ContainsKey(14))
                    return (int)ExtraData[14];
            }
            return 4;
        }

        /// <summary>
        /// Gets the stab attack.
        /// </summary>
        /// <returns></returns>
        public int GetStabAttack()
        {
            if (ExtraData != null)
            {
                if (ExtraData.ContainsKey(0))
                    return (int)ExtraData[0];
            }
            return 0;
        }

        /// <summary>
        /// Gets the slash attack.
        /// </summary>
        /// <returns></returns>
        public int GetSlashAttack()
        {
            if (ExtraData != null)
            {
                if (ExtraData.ContainsKey(1))
                    return (int)ExtraData[1];
            }
            return 0;
        }

        /// <summary>
        /// Gets the crush attack.
        /// </summary>
        /// <returns></returns>
        public int GetCrushAttack()
        {
            if (ExtraData != null)
            {
                if (ExtraData.ContainsKey(2))
                    return (int)ExtraData[2];
            }
            return 0;
        }

        /// <summary>
        /// Gets the magic attack.
        /// </summary>
        /// <returns></returns>
        public int GetMagicAttack()
        {
            if (ExtraData != null)
            {
                if (ExtraData.ContainsKey(3))
                    return (int)ExtraData[3];
            }
            return 0;
        }

        /// <summary>
        /// Gets the range attack.
        /// </summary>
        /// <returns></returns>
        public int GetRangeAttack()
        {
            if (ExtraData != null)
            {
                if (ExtraData.ContainsKey(4))
                    return (int)ExtraData[4];
            }
            return 0;
        }

        /// <summary>
        /// Gets the stab defence.
        /// </summary>
        /// <returns></returns>
        public int GetStabDefence()
        {
            if (ExtraData != null)
            {
                if (ExtraData.ContainsKey(5))
                    return (int)ExtraData[5];
            }
            return 0;
        }

        /// <summary>
        /// Gets the slash defence.
        /// </summary>
        /// <returns></returns>
        public int GetSlashDefence()
        {
            if (ExtraData != null)
            {
                if (ExtraData.ContainsKey(6))
                    return (int)ExtraData[6];
            }
            return 0;
        }

        /// <summary>
        /// Gets the crush defence.
        /// </summary>
        /// <returns></returns>
        public int GetCrushDefence()
        {
            if (ExtraData != null)
            {
                if (ExtraData.ContainsKey(7))
                    return (int)ExtraData[7];
            }
            return 0;
        }

        /// <summary>
        /// Gets the magic defence.
        /// </summary>
        /// <returns></returns>
        public int GetMagicDefence()
        {
            if (ExtraData != null)
            {
                if (ExtraData.ContainsKey(8))
                    return (int)ExtraData[8];
            }
            return 0;
        }

        /// <summary>
        /// Gets the range defence.
        /// </summary>
        /// <returns></returns>
        public int GetRangeDefence()
        {
            if (ExtraData != null)
            {
                if (ExtraData.ContainsKey(9))
                    return (int)ExtraData[9];
            }
            return 0;
        }

        /// <summary>
        /// Gets the summoning defence.
        /// </summary>
        /// <returns></returns>
        public int GetSummoningDefence()
        {
            if (ExtraData != null)
            {
                if (ExtraData.ContainsKey(417))
                    return (int)ExtraData[417];
            }
            return 0;
        }

        /// <summary>
        /// Gets the absorb melee bonus.
        /// </summary>
        /// <returns></returns>
        public int GetAbsorbMeleeBonus()
        {
            if (ExtraData != null)
            {
                if (ExtraData.ContainsKey(967))
                    return (int)ExtraData[967];
            }
            return 0;
        }

        /// <summary>
        /// Gets the absorb mage bonus.
        /// </summary>
        /// <returns></returns>
        public int GetAbsorbMageBonus()
        {
            if (ExtraData != null)
            {
                if (ExtraData.ContainsKey(969))
                    return (int)ExtraData[969];
            }
            return 0;
        }

        /// <summary>
        /// Gets the absorb range bonus.
        /// </summary>
        /// <returns></returns>
        public int GetAbsorbRangeBonus()
        {
            if (ExtraData != null)
            {
                if (ExtraData.ContainsKey(968))
                    return (int)ExtraData[968];
            }
            return 0;
        }

        /// <summary>
        /// Gets the strength bonus.
        /// </summary>
        /// <returns></returns>
        public int GetStrengthBonus()
        {
            if (ExtraData != null)
            {
                if (ExtraData.ContainsKey(641))
                    return (int)ExtraData[641] / 10;
            }
            return 0;
        }

        /// <summary>
        /// Gets the ranged strength bonus.
        /// </summary>
        /// <returns></returns>
        public int GetRangedStrengthBonus()
        {
            if (ExtraData != null)
            {
                if (ExtraData.ContainsKey(643))
                    return (int)ExtraData[643] / 10;
            }
            return 0;
        }

        /// <summary>
        /// Gets the magic damage.
        /// </summary>
        /// <returns></returns>
        public int GetMagicDamage()
        {
            if (ExtraData != null)
            {
                if (ExtraData.ContainsKey(685))
                    return (int)ExtraData[685];
            }
            return 0;
        }

        /// <summary>
        /// Gets the prayer bonus.
        /// </summary>
        /// <returns></returns>
        public int GetPrayerBonus()
        {
            if (ExtraData != null)
            {
                if (ExtraData.ContainsKey(11))
                    return (int)ExtraData[11];
            }
            return 0;
        }

        /// <summary>
        /// Get's weapon type.
        /// </summary>
        /// <returns>System.Int32.</returns>
        public int GetWeaponType()
        {
            if (ExtraData != null)
            {
                if (ExtraData.ContainsKey(686))
                    return (int)ExtraData[686];
            }
            return 0;

            /*
            switch (configuration)
            {
                case 1:
                    return new int[] { 3, 3, 3, -1 };
                case 23:
                    return new int[] { 2, 3, 2, -1 };
                case 2:
                    return new int[] { 2, 2, 3, 2 };
                case 3:
                    return new int[] { 3, 3, 3, -1 };
                case 4:
                    return new int[] { 1, 1, 3, 1 };
                case 5:
                    return new int[] { 1, 1, 2, 1 };
                case 6:
                    return new int[] { 2, 2, 1, 2 };
                case 7:
                    return new int[] {  };
            }*/
        }

        /// <summary>
        /// Get's attack bonus types of this weapon.
        /// Return's array of size 4.
        /// Stab = 1,
        /// Slash = 2,
        /// Crush = 3,
        /// Ranged = 4,
        /// Magic = 5,
        /// </summary>
        /// <returns>System.Int32[][].</returns>
        public int[] GetAttackBonusTypes()
        {
            int weaponType = GetWeaponType();
            switch (weaponType)
            {
                case 1:
                    return new int[] { 3, 3, 3, -1 };
                case 2:
                    return new int[] { 2, 2, 3, 2 };
                case 3:
                    return new int[] { 3, 3, 3, -1 };
                case 4:
                    return new int[] { 1, 1, 3, 1 };
                case 5:
                    return new int[] { 1, 1, 2, 1 };
                case 6:
                    return new int[] { 2, 2, 1, 2 };
                case 7:
                    return new int[] { 2, 2, 3, 2 };
                case 8:
                    return new int[] { 3, 3, 1, 3 };
                case 9:
                    return new int[] { 2, 2, 1, 2 };
                case 10:
                    return new int[] { 3, 3, 3, -1 };
                case 11:
                    return new int[] { 2, 2, 2, -1 };
                case 12:
                    return new int[] { 3, 3, 3, -1 };
                case 13:
                    return new int[] { 4, 4, 4, -1 };
                case 14:
                    return new int[] { 1, 2, 3, 1 };
                case 15:
                    return new int[] { 1, 2, 1, -1 };
                case 16:
                case 17:
                case 18:
                case 19:
                    return new int[] { 4, 4, 4, -1 };
                case 20:
                    return new int[] { 3, 3, -1, -1 };
                case 21:
                    return new int[] { 2, 4, 5, -1 };
                case 22:
                    return new int[] { 2, 1, 3, 2 };
                case 23:
                    return new int[] { 2, 3, 2, -1 };
                case 24:
                    return new int[] { 4, 4, 4, -1 };
                case 25:
                case 26:
                    return new int[] { 1, 2, 3, -1 };
                case 27:
                    return new int[] { 2, 1, 3, -1 };
                default:
                    return new int[] { 3, 3, 3, -1 };
            }
        }

        /// <summary>
        /// Get's attack styles types of this weapon.
        /// Return's array of size 4.
        /// MeleeAccurate = 1,
        /// MeleeAggressive = 2,
        /// MeleeDefensive = 3,
        /// MeleeControlled = 4,
        /// RangedAccurate = 5,
        /// RangedRapid = 6,
        /// RangedLongrange = 7,
        /// MagicNormal = 8,
        /// MagicDefensive = 9,
        /// </summary>
        /// <returns>System.Int32[][].</returns>
        public int[] GetAttackStylesTypes()
        {
            int weaponType = GetWeaponType();
            switch (weaponType)
            {
                case 1:
                    return new int[] { 1, 2, 3, -1 };
                case 2:
                    return new int[] { 1, 2, 2, 3 };
                case 3:
                    return new int[] { 1, 2, 3, -1 };
                case 4:
                    return new int[] { 1, 2, 2, 3 };
                case 5:
                    return new int[] { 1, 2, 2, 3 };
                case 6:
                    return new int[] { 1, 2, 4, 3 };
                case 7:
                    return new int[] { 1, 2, 2, 3 };
                case 8:
                    return new int[] { 1, 2, 4, 3 };
                case 9:
                    return new int[] { 1, 2, 4, 3 };
                case 10:
                    return new int[] { 1, 2, 3, -1 };
                case 11:
                    return new int[] { 1, 4, 3, -1 };
                case 12:
                    return new int[] { 1, 2, 3, -1 };
                case 13:
                    return new int[] { 5, 6, 7, -1 };
                case 14:
                    return new int[] { 4, 4, 4, 3 };
                case 15:
                    return new int[] { 4, 2, 3, -1 };
                case 16:
                case 17:
                case 18:
                case 19:
                    return new int[] { 5, 6, 7, -1 };
                case 20:
                    return new int[] { 2, 2, -1, -1 }; // script_1143(128, 248, -1, -1, "Aim and fire", "Aim and fire", "Kick", "Aggressive" + "<br>" + "Crush" + "<br>" + "Strength XP", "", "", "", ""); ???
                case 21: // salamander  script_1143(289, 290, 291, -1, "Scorch", "Aggressive" + "<br>" + "Slash" + "<br>" + "Strength XP", "Flare", "Accurate" + "<br>" + "Ranged" + "<br>" + "Ranged XP", "Blaze", "Defensive" + "<br>" + "Magic" + "<br>" + "Magic XP", "", "");
                    return new int[] { 2, 5, 9, -1 };
                case 22:
                    return new int[] { 1, 2, 2, 3 };
                case 23:
                    return new int[] { 1, 2, 3, -1 };
                case 24:
                    return new int[] { 5, 6, 7, -1 };
                case 25:
                case 26:
                    return new int[] { 1, 2, 3, -1 };
                case 27:
                    return new int[] { 4, 4, 4, -1 };
                default:
                    return new int[] { 1, 2, 3, -1 };
            }
        }

        /// <summary>
        /// Gets the create item requirements.
        /// </summary>
        /// <returns></returns>
        public IReadOnlyDictionary<int, int>? GetCreateItemRequirements()
        {
            if (ExtraData != null)
            {
                var requirementsBuilder = ImmutableDictionary.CreateBuilder<int, int>();
                var requiredId = -1;
                var requiredCount = -1;

                foreach (var pair in ExtraData)
                {
                    if (pair.Value is not string)
                    {
                        if (pair.Key >= 538 && pair.Key <= 770)
                        {
                            var value = Convert.ToInt32(pair.Value);
                            if (pair.Key % 2 == 0)
                            {
                                requiredId = value;
                            }
                            else
                            {
                                requiredCount = value;
                            }

                            if (requiredId != -1 && requiredCount != -1)
                            {
                                requirementsBuilder[requiredId == 1 ? requiredCount : requiredId] = requiredId == 1 ? requiredId : requiredCount;
                                requiredId = -1;
                                requiredCount = -1;
                            }
                        }
                    }
                }
                return requirementsBuilder.ToImmutable();
            }
            return null;

        }

        /// <summary>
        /// Gets the equipment level requirements.
        /// </summary>
        /// <returns>Dictionary{System.Int32System.Int32}.</returns>
        public IReadOnlyDictionary<int, int> GetEquipmentRequirements()
        {
            var requirements = ImmutableDictionary.CreateBuilder<int, int>();
            if (ExtraData != null)
            {
                for (var i = 0; i < 10; i++)
                {
                    var skillKey = 749 + (i * 2);
                    var levelKey = 750 + (i * 2);

                    if (ExtraData.TryGetValue(skillKey, out var skillObj) && ExtraData.TryGetValue(levelKey, out var levelObj))
                    {
                        if (skillObj is int skill && levelObj is int level && skill < 25 && level <= 120)
                        {
                            requirements.Add(skill, level);
                        }
                    }
                }
                if (ExtraData.TryGetValue(277, out var maxedSkillObj) && maxedSkillObj is int maxedSkill)
                {
                    if (maxedSkill >= 0 && maxedSkill <= 24)
                    {
                        requirements[maxedSkill] = Id == 19709 ? 120 : 99;
                    }
                }
            }
            return requirements.ToImmutable();
        }
    }
}
