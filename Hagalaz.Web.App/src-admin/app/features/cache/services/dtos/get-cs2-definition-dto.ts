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
