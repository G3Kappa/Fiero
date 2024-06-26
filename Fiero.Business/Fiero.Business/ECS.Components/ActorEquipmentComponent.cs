﻿using Ergo.Lang;

namespace Fiero.Business
{
    public class ActorEquipmentComponent : EcsComponent
    {
        protected readonly Dictionary<EquipmentSlotName, Equipment> Dict = new();

        [NonTerm]
        public IEnumerable<KeyValuePair<EquipmentSlotName, Equipment>> EquippedItems => Dict;
        public Weapon Weapon => Dict.Values.TrySelect(e => e.TryCast<Weapon>(out var weap) ? (true, weap) : default).SingleOrDefault();
        [NonTerm]
        public IEnumerable<Weapon> Weapons => Weapon != null ? [Weapon] : []; // TODO: refactor out
        public Armor Armor => Dict.Values.TrySelect(e => e.TryCast<Armor>(out var armor) ? (true, armor) : default).SingleOrDefault();

        public event Action<ActorEquipmentComponent> EquipmentChanged;

        public bool TryGetHeld(out Equipment leftHand, out Equipment rightHand)
        {
            var ret = Dict.TryGetValue(EquipmentSlotName.LeftHand, out leftHand);
            ret |= Dict.TryGetValue(EquipmentSlotName.RightHand, out rightHand);
            return ret;
        }

        public EquipmentSlotName? MapFreeSlot(EquipmentTypeName va) => va switch
        {
            EquipmentTypeName.Weapon when !Dict.ContainsKey(EquipmentSlotName.LeftHand)
                => EquipmentSlotName.LeftHand,
            EquipmentTypeName.Shield when !Dict.ContainsKey(EquipmentSlotName.RightHand)
                => EquipmentSlotName.RightHand,
            EquipmentTypeName.Helmet when !Dict.ContainsKey(EquipmentSlotName.Head)
                => EquipmentSlotName.Head,
            EquipmentTypeName.Armor when !Dict.ContainsKey(EquipmentSlotName.Torso)
                => EquipmentSlotName.Torso,
            EquipmentTypeName.Gauntlets when !Dict.ContainsKey(EquipmentSlotName.Arms)
                => EquipmentSlotName.Arms,
            EquipmentTypeName.Greaves when !Dict.ContainsKey(EquipmentSlotName.Legs)
                => EquipmentSlotName.Legs,
            EquipmentTypeName.Amulet when !Dict.ContainsKey(EquipmentSlotName.Neck)
                => EquipmentSlotName.Neck,
            EquipmentTypeName.Cape when !Dict.ContainsKey(EquipmentSlotName.Back)
                => EquipmentSlotName.Back,
            EquipmentTypeName.Ring when !Dict.ContainsKey(EquipmentSlotName.LeftRing)
                => EquipmentSlotName.LeftRing,
            EquipmentTypeName.Ring when !Dict.ContainsKey(EquipmentSlotName.RightRing)
                => EquipmentSlotName.RightRing,
            _ => null
        };

        public bool IsEquipped(Equipment i) => Dict.Values.Any(v => v.Id == i.Id);
        public bool MayEquip(Equipment i, out EquipmentSlotName? slot)
        {
            slot = MapFreeSlot(i.EquipmentProperties.Type);
            if (slot is null)
            {
                return false;
            }
            return true;
        }
        public bool TryEquip(Equipment i)
        {
            if (MayEquip(i, out var slot) && Dict.TryAdd(slot.Value, i))
            {
                EquipmentChanged?.Invoke(this);
                return true;
            }
            return false;
        }
        public bool TryUnequip(Equipment i)
        {
            foreach (var (k, v) in Dict)
            {
                if (v.Id == i.Id && Dict.Remove(k))
                {
                    EquipmentChanged?.Invoke(this);
                    return true;
                }
            }
            return false;
        }
    }
}
