export interface GetCs2DefinitionDto {
    id: number;
    name: string;
    intLocalsCount: number;
    stringLocalsCount: number;
    longLocalsCount: number;
    intArgsCount: number;
    stringArgsCount: number;
    longArgsCount: number;
    switchTablesCount: number;
    opcodeCount: number;
    intPoolCount: number;
    stringPoolCount: number;
    longPoolCount: number;
}

export interface GetCs2ScriptDto extends GetCs2DefinitionDto {
    executableSource?: string[];
    instructions: { index: number; opcode: number; operandKind: "byte" | "int" | "long" | "string"; operand: string }[];
    switchTables: { key: number; offset: number }[][];
    source: { index: number; text: string; label: boolean; raw: boolean; widget: { id: number; component: number } | null }[];
}
