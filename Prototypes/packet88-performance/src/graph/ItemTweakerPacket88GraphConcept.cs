using Terraria.NetWork.Core.Protocol;
using LayoutEntry = Terraria.NetWork.Concept.PacketLayoutEntry<Terraria.NetWork.Core.Protocol.ItemTweakerPacket>;

namespace Terraria.NetWork.Concept;

// Packet 88：两级 flag 依赖仍保持为一维 wire 布局。
public sealed class ItemTweakerPacket88GraphConcept
{
    public ItemTweakerPacket88GraphConcept()
    {
        var itemId = PacketNode<ItemTweakerPacket>.Field(packet => packet.ItemId);
        var flags1 = PacketNode<ItemTweakerPacket>.Field(packet => packet.Flags1);
        var colorPackedValue = PacketNode<ItemTweakerPacket>.Variable(packet => packet.ColorPackedValue);
        var damage = PacketNode<ItemTweakerPacket>.Variable(packet => packet.Damage);
        var knockBack = PacketNode<ItemTweakerPacket>.Variable(packet => packet.KnockBack);
        var useAnimation = PacketNode<ItemTweakerPacket>.Variable(packet => packet.UseAnimation);
        var useTime = PacketNode<ItemTweakerPacket>.Variable(packet => packet.UseTime);
        var shoot = PacketNode<ItemTweakerPacket>.Variable(packet => packet.Shoot);
        var shootSpeed = PacketNode<ItemTweakerPacket>.Variable(packet => packet.ShootSpeed);
        var flags2 = PacketNode<ItemTweakerPacket>.Variable(packet => packet.Flags2);
        var width = PacketNode<ItemTweakerPacket>.Variable(packet => packet.Width);
        var height = PacketNode<ItemTweakerPacket>.Variable(packet => packet.Height);
        var scale = PacketNode<ItemTweakerPacket>.Variable(packet => packet.Scale);
        var ammo = PacketNode<ItemTweakerPacket>.Variable(packet => packet.Ammo);
        var useAmmo = PacketNode<ItemTweakerPacket>.Variable(packet => packet.UseAmmo);
        var notAmmo = PacketNode<ItemTweakerPacket>.Variable(packet => packet.NotAmmo);

        var hasColorPackedValue = new PacketFlagBitCondition<ItemTweakerPacket>(flags1, 0);
        var hasDamage = new PacketFlagBitCondition<ItemTweakerPacket>(flags1, 1);
        var hasKnockBack = new PacketFlagBitCondition<ItemTweakerPacket>(flags1, 2);
        var hasUseAnimation = new PacketFlagBitCondition<ItemTweakerPacket>(flags1, 3);
        var hasUseTime = new PacketFlagBitCondition<ItemTweakerPacket>(flags1, 4);
        var hasShoot = new PacketFlagBitCondition<ItemTweakerPacket>(flags1, 5);
        var hasShootSpeed = new PacketFlagBitCondition<ItemTweakerPacket>(flags1, 6);
        var hasFlags2 = new PacketFlagBitCondition<ItemTweakerPacket>(flags1, 7);
        var hasWidth = new PacketFlagBitCondition<ItemTweakerPacket>(flags2, 0);
        var hasHeight = new PacketFlagBitCondition<ItemTweakerPacket>(flags2, 1);
        var hasScale = new PacketFlagBitCondition<ItemTweakerPacket>(flags2, 2);
        var hasAmmo = new PacketFlagBitCondition<ItemTweakerPacket>(flags2, 3);
        var hasUseAmmo = new PacketFlagBitCondition<ItemTweakerPacket>(flags2, 4);
        var hasNotAmmo = new PacketFlagBitCondition<ItemTweakerPacket>(flags2, 5);

        var colorPackedValueField = LayoutEntry.Variable(colorPackedValue, hasColorPackedValue, packet => packet.ColorPackedValue.HasValue);
        var damageField = LayoutEntry.Variable(damage, hasDamage, packet => packet.Damage.HasValue);
        var knockBackField = LayoutEntry.Variable(knockBack, hasKnockBack, packet => packet.KnockBack.HasValue);
        var useAnimationField = LayoutEntry.Variable(useAnimation, hasUseAnimation, packet => packet.UseAnimation.HasValue);
        var useTimeField = LayoutEntry.Variable(useTime, hasUseTime, packet => packet.UseTime.HasValue);
        var shootField = LayoutEntry.Variable(shoot, hasShoot, packet => packet.Shoot.HasValue);
        var shootSpeedField = LayoutEntry.Variable(shootSpeed, hasShootSpeed, packet => packet.ShootSpeed.HasValue);
        var flags2Field = LayoutEntry.Variable(flags2, hasFlags2);
        var widthField = LayoutEntry.Variable(width, [hasFlags2, hasWidth], packet => packet.Width.HasValue);
        var heightField = LayoutEntry.Variable(height, [hasFlags2, hasHeight], packet => packet.Height.HasValue);
        var scaleField = LayoutEntry.Variable(scale, [hasFlags2, hasScale], packet => packet.Scale.HasValue);
        var ammoField = LayoutEntry.Variable(ammo, [hasFlags2, hasAmmo], packet => packet.Ammo.HasValue);
        var useAmmoField = LayoutEntry.Variable(useAmmo, [hasFlags2, hasUseAmmo], packet => packet.UseAmmo.HasValue);
        var notAmmoField = LayoutEntry.Variable(notAmmo, [hasFlags2, hasNotAmmo], packet => packet.NotAmmo.HasValue);

        Layout = PacketLayout.Create(
            (byte)PacketType,
            [..LayoutEntry.Fields(itemId, flags1), colorPackedValueField, damageField, knockBackField, useAnimationField, useTimeField, shootField, shootSpeedField, flags2Field, widthField, heightField, scaleField, ammoField, useAmmoField, notAmmoField],
            NormalizeSecondLevelFlags);
    }

    public PacketType PacketType { get; } = PacketType.ItemTweaker;

    public PacketLayout<ItemTweakerPacket> Layout { get; }

    public PacketDefinitionGraph Graph => Layout.DependencyGraph;

    public void Validate(ItemTweakerPacket packet) => Layout.Validate(packet);

    public byte[] Serialize(ItemTweakerPacket packet) => Layout.Serialize(packet);

    public ItemTweakerPacket Deserialize(byte[] packetBytes) => Layout.Deserialize(packetBytes);

    private static void NormalizeSecondLevelFlags(ItemTweakerPacket packet)
    {
        if (!packet.Flags1[7])
        {
            packet.Flags2 = default;
            packet.Width = null;
            packet.Height = null;
            packet.Scale = null;
            packet.Ammo = null;
            packet.UseAmmo = null;
            packet.NotAmmo = null;
        }
    }

}
