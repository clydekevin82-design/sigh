#!/usr/bin/env python3
"""Build small LayeredFS-style RomFS overlay mods for the extracted game.

The game's BYML v7 files are not fully supported by common public Python
parsers yet, so this script performs narrow, structure-aware binary edits:
it locates root hash keys through the BYML string table and patches existing
32-bit numeric values in place. It never rewrites whole BYML documents.
"""

from __future__ import annotations

import argparse
import shutil
import struct
from dataclasses import dataclass
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
ROMFS = ROOT / "RomFS"
MODS = ROOT / "mods"


@dataclass(frozen=True)
class RootEntry:
    name: str
    value_offset: int
    node_type: int
    raw_value: int


class BymlView:
    def __init__(self, data: bytearray):
        self.data = data
        if data[:2] != b"YB":
            raise ValueError("Only little-endian BYML is supported by this patcher")
        self.version = self.u16(2)
        if self.version != 7:
            raise ValueError(f"Expected BYML v7, got v{self.version}")
        self.key_table_offset = self.u32(4)
        self.root_offset = self.u32(12)
        self.keys = self._read_string_table(self.key_table_offset)

    def u16(self, offset: int) -> int:
        return struct.unpack_from("<H", self.data, offset)[0]

    def u24(self, offset: int) -> int:
        return int.from_bytes(self.data[offset : offset + 3] + b"\0", "little")

    def u32(self, offset: int) -> int:
        return struct.unpack_from("<I", self.data, offset)[0]

    def write_i32(self, offset: int, value: int) -> None:
        struct.pack_into("<i", self.data, offset, value)

    def write_u32(self, offset: int, value: int) -> None:
        struct.pack_into("<I", self.data, offset, value)

    def _read_string_table(self, offset: int) -> list[str]:
        if self.data[offset] != 0xC2:
            raise ValueError(f"Expected string table at {offset:#x}")
        count = self.u24(offset + 1)
        strings: list[str] = []
        for index in range(count):
            string_offset = offset + self.u32(offset + 4 + 4 * index)
            end = self.data.find(b"\0", string_offset)
            strings.append(self.data[string_offset:end].decode("utf-8"))
        return strings

    def root_entries(self) -> dict[str, RootEntry]:
        if self.data[self.root_offset] != 0xC1:
            raise ValueError("Root node is not a hash")
        count = self.u24(self.root_offset + 1)
        entries: dict[str, RootEntry] = {}
        for index in range(count):
            entry_offset = self.root_offset + 4 + 8 * index
            key_index = self.u24(entry_offset)
            node_type = self.data[entry_offset + 3]
            value_offset = entry_offset + 4
            entries[self.keys[key_index]] = RootEntry(
                name=self.keys[key_index],
                value_offset=value_offset,
                node_type=node_type,
                raw_value=self.u32(value_offset),
            )
        return entries

    def patch_root_i32(self, key: str, value: int) -> None:
        entry = self.root_entries()[key]
        if entry.node_type != 0xD1:
            raise ValueError(f"{key} is node type {entry.node_type:#x}, not int")
        self.write_i32(entry.value_offset, value)

    def patch_root_u32(self, key: str, value: int) -> None:
        entry = self.root_entries()[key]
        if entry.node_type != 0xD3:
            raise ValueError(f"{key} is node type {entry.node_type:#x}, not uint")
        self.write_u32(entry.value_offset, value)

    def patch_int_array(self, array_offset: int, values: list[int]) -> None:
        if self.data[array_offset] != 0xC0:
            raise ValueError(f"Expected array at {array_offset:#x}")
        count = self.u24(array_offset + 1)
        if count != len(values):
            raise ValueError(f"Array length mismatch at {array_offset:#x}: {count} != {len(values)}")
        value_base = array_offset + 4 + ((count + 3) // 4) * 4
        for index, value in enumerate(values):
            node_type = self.data[array_offset + 4 + index]
            if node_type not in (0xD1, 0xD3):
                raise ValueError(f"Array item {index} has non-int node type {node_type:#x}")
            self.write_i32(value_base + 4 * index, value)

    def patch_hash_array_ints(self, array_key: str, row_values: list[dict[str, int]]) -> None:
        array_entry = self.root_entries()[array_key]
        array_offset = array_entry.raw_value
        if self.data[array_offset] != 0xC0:
            raise ValueError(f"{array_key} does not point to an array")
        count = self.u24(array_offset + 1)
        if count != len(row_values):
            raise ValueError(f"{array_key} row count mismatch: {count} != {len(row_values)}")
        value_base = array_offset + 4 + ((count + 3) // 4) * 4
        for row_index, values in enumerate(row_values):
            row_offset = self.u32(value_base + 4 * row_index)
            if self.data[row_offset] != 0xC1:
                raise ValueError(f"{array_key}[{row_index}] is not a hash")
            row_count = self.u24(row_offset + 1)
            for item_index in range(row_count):
                entry_offset = row_offset + 4 + 8 * item_index
                key = self.keys[self.u24(entry_offset)]
                if key not in values:
                    continue
                if self.data[entry_offset + 3] != 0xD1:
                    raise ValueError(f"{array_key}[{row_index}].{key} is not int")
                self.write_i32(entry_offset + 4, values[key])


def copy_romfs_file(mod_name: str, relative_path: str) -> Path:
    src = ROMFS / relative_path
    dst = MODS / mod_name / "romfs" / relative_path
    dst.parent.mkdir(parents=True, exist_ok=True)
    shutil.copy2(src, dst)
    return dst


def build_high_drama() -> None:
    """More active social drama: larger fight cap and harsher relationship churn."""
    mod_name = "HighDramaSocialTuning"

    trouble_path = copy_romfs_file(
        mod_name,
        "Parameter/TroubleSystem/TroubleSystem.actor__TroubleSystemParam.bgyml",
    )
    trouble = BymlView(bytearray(trouble_path.read_bytes()))
    trouble.patch_root_i32("ConfessionSecondaryRivalRate", 75)
    trouble.patch_root_i32("DepressRevengeRate", 55)
    trouble.patch_root_i32("GenerateConfessionIfRefuseRate", 80)
    trouble.patch_hash_array_ints(
        "MaxFightParam",
        [
            {"MinMiiNum": 1, "MaxMiiNum": 30, "MaxGenerateNum": 3},
            {"MinMiiNum": 31, "MaxMiiNum": 70, "MaxGenerateNum": 6},
        ],
    )
    trouble_path.write_bytes(trouble.data)

    relation_path = copy_romfs_file(
        mod_name,
        "Parameter/Relation/System.game__RelationRoot.bgyml",
    )
    relation = BymlView(bytearray(relation_path.read_bytes()))
    relation.patch_root_i32("BgProcessBadJudgeMeterDiff", -75)
    relation.patch_root_i32("GroupActionRate", 55)
    relation.patch_int_array(0x1E0, [40, 100, 60])
    relation_path.write_bytes(relation.data)


def build_group_activity_plus() -> None:
    """More background group interactions without increasing fight tuning."""
    mod_name = "GroupActivityPlus"
    relation_path = copy_romfs_file(
        mod_name,
        "Parameter/Relation/System.game__RelationRoot.bgyml",
    )
    relation = BymlView(bytearray(relation_path.read_bytes()))
    relation.patch_root_i32("GroupActionRate", 85)
    relation_path.write_bytes(relation.data)


def build_all() -> None:
    build_high_drama()
    build_group_activity_plus()


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument(
        "mod",
        nargs="?",
        default="all",
        choices=["all", "high-drama", "group-activity-plus"],
    )
    args = parser.parse_args()

    if args.mod == "all":
        build_all()
    elif args.mod == "high-drama":
        build_high_drama()
    elif args.mod == "group-activity-plus":
        build_group_activity_plus()


if __name__ == "__main__":
    main()
