using System.Collections.Generic;
using Hagalaz.Data.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Hagalaz.Data
{
    public class HagalazDbContext : IdentityDbContext<Character, Aspnetrole, uint, Aspnetuserclaim, Aspnetuserrole, Aspnetuserlogin, Aspnetroleclaim,
        Aspnetusertoken>
    {
        public HagalazDbContext(DbContextOptions<HagalazDbContext> options) : base(options) { }

        public virtual DbSet<Area> Areas { get; set; } = null!;
        public virtual DbSet<Aspnetrole> Aspnetroles { get; set; } = null!;
        public virtual DbSet<Aspnetroleclaim> Aspnetroleclaims { get; set; } = null!;
        public virtual DbSet<Aspnetuserclaim> Aspnetuserclaims { get; set; } = null!;
        public virtual DbSet<Aspnetuserlogin> Aspnetuserlogins { get; set; } = null!;
        public virtual DbSet<Aspnetusertoken> Aspnetusertokens { get; set; } = null!;
        public virtual DbSet<Blacklist> Blacklists { get; set; } = null!;
        public virtual DbSet<Character> Characters { get; set; } = null!;
        public virtual DbSet<CharactersProfile> CharacterProfiles { get; set; } = null!;
        public virtual DbSet<CharactersAppeal> CharactersAppeals { get; set; } = null!;
        public virtual DbSet<CharactersContact> CharactersContacts { get; set; } = null!;
        public virtual DbSet<CharactersFamiliar> CharactersFamiliars { get; set; } = null!;
        public virtual DbSet<CharactersFarmingPatch> CharactersFarmingPatches { get; set; } = null!;
        public virtual DbSet<CharactersItem> CharactersItems { get; set; } = null!;
        public virtual DbSet<CharactersItemsLook> CharactersItemsLooks { get; set; } = null!;
        public virtual DbSet<CharactersLook> CharactersLooks { get; set; } = null!;
        public virtual DbSet<CharactersMusic> CharactersMusics { get; set; } = null!;
        public virtual DbSet<CharactersMusicPlaylist> CharactersMusicPlaylists { get; set; } = null!;
        public virtual DbSet<CharactersNote> CharactersNotes { get; set; } = null!;
        public virtual DbSet<CharactersOffence> CharactersOffences { get; set; } = null!;
        public virtual DbSet<CharactersPermission> CharactersPermissions { get; set; } = null!;
        public virtual DbSet<CharactersPreference> CharactersPreferences { get; set; } = null!;
        public virtual DbSet<CharactersQuest> CharactersQuests { get; set; } = null!;
        public virtual DbSet<CharactersReport> CharactersReports { get; set; } = null!;
        public virtual DbSet<CharactersReward> CharactersRewards { get; set; } = null!;
        public virtual DbSet<CharactersSlayerTask> CharactersSlayerTasks { get; set; } = null!;
        public virtual DbSet<CharactersState> CharactersStates { get; set; } = null!;
        public virtual DbSet<CharactersStatistic> CharactersStatistics { get; set; } = null!;
        public virtual DbSet<CharactersTicket> CharactersTickets { get; set; } = null!;
        public virtual DbSet<CharactersVariable> CharactersVariables { get; set; } = null!;
        public virtual DbSet<CharacterscreateinfoItem> CharacterscreateinfoItems { get; set; } = null!;
        public virtual DbSet<Clan> Clans { get; set; } = null!;
        public virtual DbSet<ClansMember> ClansMembers { get; set; } = null!;
        public virtual DbSet<ClansSetting> ClansSettings { get; set; } = null!;
        public virtual DbSet<Configuration> Configurations { get; set; } = null!;
        public virtual DbSet<Efmigrationshistory> Efmigrationshistories { get; set; } = null!;
        public virtual DbSet<EquipmentBonuse> EquipmentBonuses { get; set; } = null!;
        public virtual DbSet<EquipmentDefinition> EquipmentDefinitions { get; set; } = null!;
        public virtual DbSet<GameEvent> GameEvents { get; set; } = null!;
        public virtual DbSet<GameobjectDefinition> GameobjectDefinitions { get; set; } = null!;
        public virtual DbSet<GameobjectLodestone> GameobjectLodestones { get; set; } = null!;
        public virtual DbSet<GameobjectLoot> GameobjectLoots { get; set; } = null!;
        public virtual DbSet<GameobjectLootItem> GameobjectLootItems { get; set; } = null!;
        public virtual DbSet<GameobjectSpawn> GameobjectSpawns { get; set; } = null!;
        public virtual DbSet<ItemCombine> ItemCombines { get; set; } = null!;
        public virtual DbSet<ItemDefinition> ItemDefinitions { get; set; } = null!;
        public virtual DbSet<ItemLoot> ItemLoots { get; set; } = null!;
        public virtual DbSet<ItemLootItem> ItemLootItems { get; set; } = null!;
        public virtual DbSet<ItemSpawn> ItemSpawns { get; set; } = null!;
        public virtual DbSet<LogsActivity> LogsActivities { get; set; } = null!;
        public virtual DbSet<LogsChat> LogsChats { get; set; } = null!;
        public virtual DbSet<LogsConnection> LogsConnections { get; set; } = null!;
        public virtual DbSet<LogsDisplayNameChange> LogsDisplayNameChanges { get; set; } = null!;
        public virtual DbSet<LogsLoginAttempt> LogsLoginAttempts { get; set; } = null!;
        public virtual DbSet<MinigamesBarrow> MinigamesBarrows { get; set; } = null!;
        public virtual DbSet<MinigamesDuelArena> MinigamesDuelArenas { get; set; } = null!;
        public virtual DbSet<MinigamesGodwar> MinigamesGodwars { get; set; } = null!;
        public virtual DbSet<MinigamesTzhaarCave> MinigamesTzhaarCaves { get; set; } = null!;
        public virtual DbSet<MinigamesTzhaarCaveWave> MinigamesTzhaarCaveWaves { get; set; } = null!;
        public virtual DbSet<MusicDefinition> MusicDefinitions { get; set; } = null!;
        public virtual DbSet<MusicLocation> MusicLocations { get; set; } = null!;
        public virtual DbSet<NpcBonuses> NpcBonuses { get; set; } = null!;
        public virtual DbSet<NpcDefinition> NpcDefinitions { get; set; } = null!;
        public virtual DbSet<NpcLoot> NpcLoots { get; set; } = null!;
        public virtual DbSet<NpcLootItem> NpcLootItems { get; set; } = null!;
        public virtual DbSet<NpcSpawn> NpcSpawns { get; set; } = null!;
        public virtual DbSet<NpcStatistic> NpcStatistics { get; set; } = null!;
        public virtual DbSet<ProfanityWord> ProfanityWords { get; set; } = null!;
        public virtual DbSet<Quest> Quests { get; set; } = null!;
        public virtual DbSet<ReservedName> ReservedNames { get; set; } = null!;
        public virtual DbSet<Shop> Shops { get; set; } = null!;
        public virtual DbSet<SkillsCookingFoodDefinition> SkillsCookingFoodDefinitions { get; set; } = null!;
        public virtual DbSet<SkillsCookingRawFoodDefinition> SkillsCookingRawFoodDefinitions { get; set; } = null!;
        public virtual DbSet<SkillsCraftingGemDefinition> SkillsCraftingGemDefinitions { get; set; } = null!;
        public virtual DbSet<SkillsCraftingJewelryDefinition> SkillsCraftingJewelryDefinitions { get; set; } = null!;
        public virtual DbSet<SkillsCraftingLeatherDefinition> SkillsCraftingLeatherDefinitions { get; set; } = null!;
        public virtual DbSet<SkillsCraftingPotteryDefinition> SkillsCraftingPotteryDefinitions { get; set; } = null!;
        public virtual DbSet<SkillsCraftingSilverDefinition> SkillsCraftingSilverDefinitions { get; set; } = null!;
        public virtual DbSet<SkillsCraftingSpinDefinition> SkillsCraftingSpinDefinitions { get; set; } = null!;
        public virtual DbSet<SkillsCraftingTanDefinition> SkillsCraftingTanDefinitions { get; set; } = null!;
        public virtual DbSet<SkillsFarmingPatchDefinition> SkillsFarmingPatchDefinitions { get; set; } = null!;
        public virtual DbSet<SkillsFarmingSeedDefinition> SkillsFarmingSeedDefinitions { get; set; } = null!;
        public virtual DbSet<SkillsFiremakingDefinition> SkillsFiremakingDefinitions { get; set; } = null!;
        public virtual DbSet<SkillsFishingFishDefinition> SkillsFishingFishDefinitions { get; set; } = null!;
        public virtual DbSet<SkillsFishingSpotDefinition> SkillsFishingSpotDefinitions { get; set; } = null!;
        public virtual DbSet<SkillsFishingSpotNpcDefinition> SkillsFishingSpotNpcDefinitions { get; set; } = null!;
        public virtual DbSet<SkillsFishingToolDefinition> SkillsFishingToolDefinitions { get; set; } = null!;
        public virtual DbSet<SkillsFletchingDefinition> SkillsFletchingDefinitions { get; set; } = null!;
        public virtual DbSet<SkillsHerbloreHerbDefinition> SkillsHerbloreHerbDefinitions { get; set; } = null!;
        public virtual DbSet<SkillsMagicCombatDefinition> SkillsMagicCombatDefinitions { get; set; } = null!;
        public virtual DbSet<SkillsMagicEnchantDefinition> SkillsMagicEnchantDefinitions { get; set; } = null!;
        public virtual DbSet<SkillsMagicEnchantProduct> SkillsMagicEnchantProducts { get; set; } = null!;
        public virtual DbSet<SkillsMagicTeleportDefinition> SkillsMagicTeleportDefinitions { get; set; } = null!;
        public virtual DbSet<SkillsMiningOreDefinition> SkillsMiningOreDefinitions { get; set; } = null!;
        public virtual DbSet<SkillsMiningPickaxeDefinition> SkillsMiningPickaxeDefinitions { get; set; } = null!;
        public virtual DbSet<SkillsMiningRockDefinition> SkillsMiningRockDefinitions { get; set; } = null!;
        public virtual DbSet<SkillsPrayerDefinition> SkillsPrayerDefinitions { get; set; } = null!;
        public virtual DbSet<SkillsRunecraftingDefinition> SkillsRunecraftingDefinitions { get; set; } = null!;
        public virtual DbSet<SkillsSlayerMasterDefinition> SkillsSlayerMasterDefinitions { get; set; } = null!;
        public virtual DbSet<SkillsSlayerTaskDefinition> SkillsSlayerTaskDefinitions { get; set; } = null!;
        public virtual DbSet<SkillsSummoningDefinition> SkillsSummoningDefinitions { get; set; } = null!;
        public virtual DbSet<SkillsWoodcuttingHatchetDefinition> SkillsWoodcuttingHatchetDefinitions { get; set; } = null!;
        public virtual DbSet<SkillsWoodcuttingLogDefinition> SkillsWoodcuttingLogDefinitions { get; set; } = null!;
        public virtual DbSet<SkillsWoodcuttingTreeDefinition> SkillsWoodcuttingTreeDefinitions { get; set; } = null!;
        public virtual DbSet<World> Worlds { get; set; } = null!;
        public virtual DbSet<WorldConfiguration> WorldConfigurations { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.UseCollation("utf8mb4_0900_ai_ci").HasCharSet("utf8mb4", DelegationModes.ApplyToAll);

            modelBuilder.Entity<Area>(entity =>
            {
                entity.ToTable("areas");

                entity.HasCharSet("latin1").UseCollation("latin1_swedish_ci");

                entity.Property(e => e.Id).HasColumnType("smallint(6) unsigned").HasColumnName("id");

                entity.Property(e => e.CannonAllowed).HasColumnType("tinyint(1) unsigned").HasColumnName("cannon_allowed").HasDefaultValueSql("'1'");

                entity.Property(e => e.FamiliarAllowed).HasColumnType("tinyint(1) unsigned").HasColumnName("familiar_allowed").HasDefaultValueSql("'1'");

                entity.Property(e => e.MaximumDimension).HasColumnType("smallint(6) unsigned").HasColumnName("maximum_dimension");

                entity.Property(e => e.MaximumX).HasColumnType("smallint(6)").HasColumnName("maximum_x");

                entity.Property(e => e.MaximumY).HasColumnType("smallint(6)").HasColumnName("maximum_y");

                entity.Property(e => e.MaximumZ).HasColumnType("tinyint(1) unsigned").HasColumnName("maximum_z");

                entity.Property(e => e.MinimumDimension).HasColumnType("smallint(6) unsigned").HasColumnName("minimum_dimension");

                entity.Property(e => e.MinimumX).HasColumnType("smallint(6)").HasColumnName("minimum_x");

                entity.Property(e => e.MinimumY).HasColumnType("smallint(6)").HasColumnName("minimum_y");

                entity.Property(e => e.MinimumZ).HasColumnType("tinyint(1) unsigned").HasColumnName("minimum_z");

                entity.Property(e => e.MulticombatAllowed).HasColumnType("tinyint(1) unsigned").HasColumnName("multicombat_allowed");

                entity.Property(e => e.Name).HasMaxLength(40).HasColumnName("name").HasDefaultValueSql("''");

                entity.Property(e => e.PvpAllowed).HasColumnType("tinyint(1) unsigned").HasColumnName("pvp_allowed");
            });

            modelBuilder.Entity<Aspnetrole>(entity =>
            {
                entity.ToTable("aspnetroles");

                entity.HasIndex(e => e.NormalizedName, "RoleNameIndex").IsUnique();

                entity.Property(e => e.Id).HasColumnType("int(11) unsigned");

                entity.Property(e => e.Name).HasMaxLength(256);

                entity.Property(e => e.NormalizedName).HasMaxLength(256);
            });

            modelBuilder.Entity<Aspnetroleclaim>(entity =>
            {
                entity.ToTable("aspnetroleclaims");

                entity.HasIndex(e => e.RoleId, "IX_AspNetRoleClaims_RoleId");

                entity.Property(e => e.Id).HasColumnType("int(11)");

                entity.Property(e => e.RoleId).HasColumnType("int(11) unsigned");

                entity.HasOne(d => d.Role)
                    .WithMany(p => p.Aspnetroleclaims)
                    .HasForeignKey(d => d.RoleId)
                    .HasConstraintName("FK_AspNetRoleClaims_AspNetRoles_RoleId");
            });

            modelBuilder.Entity<Aspnetuserclaim>(entity =>
            {
                entity.ToTable("aspnetuserclaims");

                entity.HasIndex(e => e.UserId, "IX_AspNetUserClaims_UserId");

                entity.Property(e => e.Id).HasColumnType("int(11)");

                entity.Property(e => e.UserId).HasColumnType("int(11) unsigned");

                entity.HasOne(d => d.User)
                    .WithMany(p => p.Aspnetuserclaims)
                    .HasForeignKey(d => d.UserId)
                    .HasConstraintName("FK_AspNetUserClaims_characters_UserId");
            });

            modelBuilder.Entity<Aspnetuserlogin>(entity =>
            {
                entity.HasKey(e => new
                    {
                        e.LoginProvider, e.ProviderKey
                    })
                    .HasName("PRIMARY")
                    .HasAnnotation("MySql:IndexPrefixLength",
                        new[]
                        {
                            0, 0
                        });

                entity.ToTable("aspnetuserlogins");

                entity.HasIndex(e => e.UserId, "IX_AspNetUserLogins_UserId");

                entity.Property(e => e.UserId).HasColumnType("int(11) unsigned");

                entity.HasOne(d => d.User)
                    .WithMany(p => p.Aspnetuserlogins)
                    .HasForeignKey(d => d.UserId)
                    .HasConstraintName("FK_AspNetUserLogins_characters_UserId");
            });

            modelBuilder.Entity<Aspnetusertoken>(entity =>
            {
                entity.HasKey(e => new
                    {
                        e.UserId, e.LoginProvider, e.Name
                    })
                    .HasName("PRIMARY")
                    .HasAnnotation("MySql:IndexPrefixLength",
                        new[]
                        {
                            0, 0, 0
                        });

                entity.ToTable("aspnetusertokens");

                entity.Property(e => e.UserId).HasColumnType("int(11) unsigned");

                entity.HasOne(d => d.User)
                    .WithMany(p => p.Aspnetusertokens)
                    .HasForeignKey(d => d.UserId)
                    .HasConstraintName("FK_AspNetUserTokens_characters_UserId");
            });

            modelBuilder.Entity<Aspnetuserrole>(entity =>
            {
                entity.HasKey(r => new
                {
                    r.UserId, r.RoleId
                });
                entity.ToTable("aspnetuserroles");

                entity.Property(e => e.UserId).HasColumnType("int(11) unsigned");
                entity.Property(e => e.RoleId).HasColumnType("int(11) unsigned");

                entity.HasOne(d => d.Role)
                    .WithMany(d => d.Aspnetuserroles)
                    .HasForeignKey(d => d.RoleId)
                    .HasConstraintName("FK_AspNetUserRoles_AspNetRoles_RoleId");
                entity.HasOne(d => d.User)
                    .WithMany(d => d.Aspnetuserroles)
                    .HasForeignKey(d => d.UserId)
                    .HasConstraintName("FK_AspNetUserRoles_characters_UserId");

                entity.HasIndex(e => e.RoleId, "IX_AspNetUserRoles_RoleId");
            });

            modelBuilder.Entity<Blacklist>(entity =>
            {
                entity.ToTable("blacklist");

                entity.HasCharSet("latin1").UseCollation("latin1_swedish_ci");

                entity.HasIndex(e => e.AppealId, "appeal_id_foreign_key");

                entity.HasIndex(e => e.ModeratorId, "moderator_id_foreign_key_2");

                entity.Property(e => e.Id).HasColumnType("int(10) unsigned").HasColumnName("id");

                entity.Property(e => e.AppealId).HasColumnType("int(11) unsigned").HasColumnName("appeal_id");

                entity.Property(e => e.Date).HasColumnType("datetime").HasColumnName("date");

                entity.Property(e => e.IpAddress).HasMaxLength(40).HasColumnName("ip_address").HasDefaultValueSql("''");

                entity.Property(e => e.ModeratorId).HasColumnType("int(11) unsigned").HasColumnName("moderator_id");

                entity.Property(e => e.Reason).HasColumnType("text").HasColumnName("reason");

                entity.HasOne(d => d.Appeal)
                    .WithMany(p => p.Blacklists)
                    .HasForeignKey(d => d.AppealId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("appeal_id_foreign_key");

                entity.HasOne(d => d.Moderator)
                    .WithMany(p => p.Blacklists)
                    .HasForeignKey(d => d.ModeratorId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("moderator_id_foreign_key_2");
            });

            modelBuilder.Entity<Character>(entity =>
            {
                entity.ToTable("characters");

                entity.HasCharSet("latin1").UseCollation("latin1_swedish_ci");

                entity.HasIndex(e => e.NormalizedEmail, "EmailIndex");

                entity.HasIndex(e => e.NormalizedUserName, "UserNameIndex").IsUnique();

                entity.HasIndex(e => e.DisplayName, "display_name").IsUnique();

                entity.HasIndex(e => e.Id, "master_id");

                entity.Property(e => e.Id).HasColumnType("int(11) unsigned").HasColumnName("id");

                entity.Property(e => e.AccessFailedCount).HasColumnType("int(11)");

                entity.Property(e => e.Birthyear).HasColumnType("smallint(6)").HasColumnName("birthyear");

                entity.Property(e => e.CoordX).HasColumnType("smallint(6)").HasColumnName("coord_x").HasDefaultValueSql("'3100'");

                entity.Property(e => e.CoordY).HasColumnType("smallint(6)").HasColumnName("coord_y").HasDefaultValueSql("'3499'");

                entity.Property(e => e.CoordZ).HasColumnType("tinyint(3) unsigned").HasColumnName("coord_z");

                entity.Property(e => e.DisplayName).HasMaxLength(12).HasColumnName("display_name").HasDefaultValueSql("'#Player'");

                entity.Property(e => e.DisplayNameLastChanged).HasColumnType("datetime").HasColumnName("display_name_last_changed");

                entity.Property(e => e.Email).HasMaxLength(255).HasColumnName("email").HasDefaultValueSql("''");

                entity.Property(e => e.EmailConfirmed).HasColumnType("bit(1)").HasDefaultValueSql("b'0'");

                entity.Property(e => e.LastGameLogin).HasColumnType("datetime").HasColumnName("last_game_login");

                entity.Property(e => e.LastIp).HasMaxLength(40).HasColumnName("last_ip");

                entity.Property(e => e.LastLobbyLogin).HasColumnType("datetime").HasColumnName("last_lobby_login");

                entity.Property(e => e.LockoutEnabled).HasColumnType("bit(1)").HasDefaultValueSql("b'0'");

                entity.Property(e => e.LockoutEnd).HasMaxLength(6);

                entity.Property(e => e.NormalizedEmail).HasMaxLength(256);

                entity.Property(e => e.NormalizedUserName).HasMaxLength(256);

                entity.Property(e => e.Password).HasColumnName("password");

                entity.Ignore(e => e.Password);

                entity.Property(e => e.PasswordHash).HasColumnName("password");

                entity.Property(e => e.PhoneNumberConfirmed).HasColumnType("bit(1)").HasDefaultValueSql("b'0'");

                entity.Property(e => e.PreviousDisplayName).HasMaxLength(12).HasColumnName("previous_display_name");

                entity.Property(e => e.RegisterDate).HasColumnType("datetime").HasColumnName("register_date");

                entity.Property(e => e.RegisterIp).HasMaxLength(40).HasColumnName("register_ip");

                entity.Property(e => e.TwoFactorEnabled).HasColumnType("bit(1)").HasDefaultValueSql("b'0'");

                entity.Property(e => e.UserName).HasColumnType("varchar(32)").HasMaxLength(24).HasColumnName("username");
            });

            modelBuilder.Entity<CharactersProfile>(entity =>
            {
                entity.HasKey(e => e.MasterId).HasName("PRIMARY");

                entity.ToTable("characters_profiles");

                entity.HasCharSet("latin1").UseCollation("latin1_swedish_ci");

                entity.HasIndex(e => e.MasterId, "master_foreign_key_24");

                entity.Property(e => e.MasterId).HasColumnType("int(11) unsigned").HasColumnName("master_id");

                entity.Property(e => e.Data).HasColumnType("json").HasColumnName("data");

                entity.HasOne(d => d.Master)
                    .WithOne(p => p.CharacterProfile)
                    .HasForeignKey<CharactersProfile>(d => d.MasterId)
                    .HasConstraintName("master_id_foreign_key_24");
            });

            modelBuilder.Entity<CharactersAppeal>(entity =>
            {
                entity.ToTable("characters_appeals");

                entity.HasCharSet("latin1").UseCollation("latin1_swedish_ci");

                entity.HasIndex(e => e.MasterId, "master_foreign_key");

                entity.Property(e => e.Id).HasColumnType("int(11) unsigned").HasColumnName("id");

                entity.Property(e => e.Data).HasColumnType("text").HasColumnName("data");

                entity.Property(e => e.MasterId).HasColumnType("int(11) unsigned").HasColumnName("master_id");

                entity.HasOne(d => d.Master)
                    .WithMany(p => p.CharactersAppeals)
                    .HasForeignKey(d => d.MasterId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("master_foreign_key");
            });

            modelBuilder.Entity<CharactersContact>(entity =>
            {
                entity.HasKey(e => new
                    {
                        e.MasterId, e.ContactId
                    })
                    .HasName("PRIMARY")
                    .HasAnnotation("MySql:IndexPrefixLength",
                        new[]
                        {
                            0, 0
                        });

                entity.ToTable("characters_contacts");

                entity.HasCharSet("latin1").UseCollation("latin1_swedish_ci");

                entity.HasIndex(e => e.ContactId, "contact_id_foreign_key");

                entity.HasIndex(e => e.MasterId, "master_id_foreign_key_14");

                entity.Property(e => e.MasterId).HasColumnType("int(11) unsigned").HasColumnName("master_id");

                entity.Property(e => e.ContactId).HasColumnType("int(11) unsigned").HasColumnName("contact_id");

                entity.Property(e => e.FcRank).HasColumnType("tinyint(3)").HasColumnName("fc_rank");

                entity.Property(e => e.Type).HasColumnType("tinyint(3) unsigned").HasColumnName("type");

                entity.HasOne(d => d.Contact)
                    .WithMany(p => p.CharactersContactContacts)
                    .HasForeignKey(d => d.ContactId)
                    .HasConstraintName("contact_id_foreign_key");

                entity.HasOne(d => d.Master)
                    .WithMany(p => p.CharactersContactMasters)
                    .HasForeignKey(d => d.MasterId)
                    .HasConstraintName("master_id_foreign_key_14");
            });

            modelBuilder.Entity<CharactersFamiliar>(entity =>
            {
                entity.HasKey(e => e.MasterId).HasName("PRIMARY");

                entity.ToTable("characters_familiars");

                entity.HasCharSet("latin1").UseCollation("latin1_swedish_ci");

                entity.HasIndex(e => e.FamiliarId, "familiar_id_foreign_key");

                entity.Property(e => e.MasterId).HasColumnType("int(11) unsigned").ValueGeneratedNever().HasColumnName("master_id");

                entity.Property(e => e.FamiliarId).HasColumnType("smallint(6) unsigned").HasColumnName("familiar_id");

                entity.Property(e => e.SpecialMovePoints).HasColumnType("smallint(6) unsigned").HasColumnName("special_move_points").HasDefaultValueSql("'60'");

                entity.Property(e => e.TicksRemaining).HasColumnType("int(11) unsigned").HasColumnName("ticks_remaining");

                entity.Property(e => e.UsingSpecialMove).HasColumnType("tinyint(1) unsigned").HasColumnName("using_special_move");

                entity.HasOne(d => d.Familiar)
                    .WithMany(p => p.CharactersFamiliars)
                    .HasForeignKey(d => d.FamiliarId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("familiar_id_foreign_key");

                entity.HasOne(d => d.Master)
                    .WithOne(p => p.CharactersFamiliar)
                    .HasForeignKey<CharactersFamiliar>(d => d.MasterId)
                    .HasConstraintName("master_id_foreign_key");
            });

            modelBuilder.Entity<CharactersFarmingPatch>(entity =>
            {
                entity.HasKey(e => new
                    {
                        e.MasterId, e.PatchId
                    })
                    .HasName("PRIMARY")
                    .HasAnnotation("MySql:IndexPrefixLength",
                        new[]
                        {
                            0, 0
                        });

                entity.ToTable("characters_farming_patches");

                entity.HasCharSet("utf8").UseCollation("utf8_general_ci");

                entity.HasIndex(e => e.PatchId, "farming_patch_foreign_key");

                entity.HasIndex(e => e.SeedId, "seed_id_foreign_key");

                entity.Property(e => e.MasterId).HasColumnType("int(11) unsigned").HasColumnName("master_id");

                entity.Property(e => e.PatchId).HasColumnType("int(11) unsigned").HasColumnName("patch_id");

                entity.Property(e => e.ConditionFlag).HasColumnType("int(11) unsigned").HasColumnName("condition_flag");

                entity.Property(e => e.CurrentCycle).HasColumnType("tinyint(3) unsigned").HasColumnName("current_cycle");

                entity.Property(e => e.CurrentCycleTicks).HasColumnType("int(11) unsigned").HasColumnName("current_cycle_ticks");

                entity.Property(e => e.ProductCount).HasColumnType("int(11) unsigned").HasColumnName("product_count");

                entity.Property(e => e.SeedId).HasColumnType("smallint(6) unsigned").HasColumnName("seed_id");

                entity.HasOne(d => d.Master)
                    .WithMany(p => p.CharactersFarmingPatches)
                    .HasForeignKey(d => d.MasterId)
                    .HasConstraintName("master_id_foreign_key_1");

                entity.HasOne(d => d.Patch)
                    .WithMany(p => p.CharactersFarmingPatches)
                    .HasForeignKey(d => d.PatchId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("farming_patch_foreign_key");

                entity.HasOne(d => d.Seed)
                    .WithMany(p => p.CharactersFarmingPatches)
                    .HasForeignKey(d => d.SeedId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("seed_id_foreign_key");
            });

            modelBuilder.Entity<CharactersItem>(entity =>
            {
                entity.ToTable("characters_items");

                entity.HasCharSet("latin1").UseCollation("latin1_swedish_ci");

                entity.HasIndex(e => e.MasterId, "master_id_foreign_key_15");

                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.ContainerType).HasColumnName("container_type");

                entity.Property(e => e.Count).HasColumnName("count").HasDefaultValueSql("'1'");

                entity.Property(e => e.ExtraData).HasColumnName("extra_data").UseCollation("utf8mb4_0900_ai_ci").HasCharSet("utf8mb4");

                entity.Property(e => e.ItemId).HasColumnName("item_id");

                entity.Property(e => e.MasterId).HasColumnName("master_id");

                entity.Property(e => e.SlotId).HasColumnName("slot_id");

                entity.HasOne(d => d.Master).WithMany(p => p.CharactersItems).HasForeignKey(d => d.MasterId).HasConstraintName("master_id_foreign_key_15");
            });

            modelBuilder.Entity<CharactersItemsLook>(entity =>
            {
                entity.HasKey(e => new
                    {
                        e.MasterId, e.ItemId
                    })
                    .HasName("PRIMARY")
                    .HasAnnotation("MySql:IndexPrefixLength",
                        new[]
                        {
                            0, 0
                        });

                entity.ToTable("characters_items_look");

                entity.HasCharSet("latin1").UseCollation("latin1_swedish_ci");

                entity.Property(e => e.MasterId).HasColumnName("master_id");

                entity.Property(e => e.ItemId).HasColumnName("item_id");

                entity.Property(e => e.FemaleWornModel1).HasColumnName("female_worn_model_1").HasDefaultValueSql("'-1'");

                entity.Property(e => e.FemaleWornModel2).HasColumnName("female_worn_model_2").HasDefaultValueSql("'-1'");

                entity.Property(e => e.FemaleWornModel3).HasColumnName("female_worn_model_3").HasDefaultValueSql("'-1'");

                entity.Property(e => e.MaleWornModel1).HasColumnName("male_worn_model_1").HasDefaultValueSql("'-1'");

                entity.Property(e => e.MaleWornModel2).HasColumnName("male_worn_model_2").HasDefaultValueSql("'-1'");

                entity.Property(e => e.MaleWornModel3).HasColumnName("male_worn_model_3").HasDefaultValueSql("'-1'");

                entity.Property(e => e.ModelColours).HasColumnName("model_colours").UseCollation("utf8mb4_0900_ai_ci").HasCharSet("utf8mb4");

                entity.Property(e => e.TextureColours).HasColumnName("texture_colours").UseCollation("utf8mb4_0900_ai_ci").HasCharSet("utf8mb4");

                entity.HasOne(d => d.Master).WithMany(p => p.CharactersItemsLooks).HasForeignKey(d => d.MasterId).HasConstraintName("master_id_foreign_key_2");
            });

            modelBuilder.Entity<CharactersLook>(entity =>
            {
                entity.HasKey(e => e.MasterId).HasName("PRIMARY");

                entity.ToTable("characters_look");

                entity.HasCharSet("latin1").UseCollation("latin1_swedish_ci");

                entity.HasIndex(e => e.MasterId, "LOOK_MASTER_ID_FOREIGN");

                entity.Property(e => e.MasterId).HasColumnType("int(11) unsigned").ValueGeneratedNever().HasColumnName("master_id");

                entity.Property(e => e.ArmsLook).HasColumnType("int(10)").HasColumnName("arms_look");

                entity.Property(e => e.BeardLook).HasColumnType("int(10)").HasColumnName("beard_look");

                entity.Property(e => e.DisplayTitle).HasColumnType("tinyint(4) unsigned").HasColumnName("display_title");

                entity.Property(e => e.FeetColor).HasColumnType("smallint(6)").HasColumnName("feet_color");

                entity.Property(e => e.FeetLook).HasColumnType("int(10)").HasColumnName("feet_look");

                entity.Property(e => e.Gender).HasColumnType("smallint(6)").HasColumnName("gender");

                entity.Property(e => e.HairColor).HasColumnType("smallint(6)").HasColumnName("hair_color");

                entity.Property(e => e.HairLook).HasColumnType("int(10)").HasColumnName("hair_look");

                entity.Property(e => e.LegColor).HasColumnType("smallint(6)").HasColumnName("leg_color");

                entity.Property(e => e.LegsLook).HasColumnType("int(10)").HasColumnName("legs_look");

                entity.Property(e => e.SkinColor).HasColumnType("smallint(6)").HasColumnName("skin_color");

                entity.Property(e => e.TorsoColor).HasColumnType("smallint(6)").HasColumnName("torso_color");

                entity.Property(e => e.TorsoLook).HasColumnType("int(10)").HasColumnName("torso_look");

                entity.Property(e => e.WristLook).HasColumnType("int(10)").HasColumnName("wrist_look");

                entity.HasOne(d => d.Master)
                    .WithOne(p => p.CharactersLook)
                    .HasForeignKey<CharactersLook>(d => d.MasterId)
                    .HasConstraintName("master_id_foreign_key_13");
            });

            modelBuilder.Entity<CharactersMusic>(entity =>
            {
                entity.HasKey(e => e.MasterId).HasName("PRIMARY");

                entity.ToTable("characters_music");

                entity.HasCharSet("utf8").UseCollation("utf8_general_ci");

                entity.Property(e => e.MasterId).HasColumnType("int(11) unsigned").ValueGeneratedNever().HasColumnName("master_id");

                entity.Property(e => e.UnlockedMusic).HasColumnType("text").HasColumnName("unlocked_music");

                entity.HasOne(d => d.Master)
                    .WithOne(p => p.CharactersMusic)
                    .HasForeignKey<CharactersMusic>(d => d.MasterId)
                    .HasConstraintName("master_id_foreign_key_3");
            });

            modelBuilder.Entity<CharactersMusicPlaylist>(entity =>
            {
                entity.HasKey(e => e.MasterId).HasName("PRIMARY");

                entity.ToTable("characters_music_playlists");

                entity.HasCharSet("utf8").UseCollation("utf8_general_ci");

                entity.Property(e => e.MasterId).HasColumnType("int(11) unsigned").ValueGeneratedNever().HasColumnName("master_id");

                entity.Property(e => e.Playlist).HasMaxLength(56).HasColumnName("playlist").HasDefaultValueSql("''");

                entity.Property(e => e.PlaylistToggled).HasColumnType("tinyint(3) unsigned").HasColumnName("playlist_toggled");

                entity.Property(e => e.ShuffleToggled).HasColumnType("tinyint(3) unsigned").HasColumnName("shuffle_toggled");

                entity.HasOne(d => d.Master)
                    .WithOne(p => p.CharactersMusicPlaylist)
                    .HasForeignKey<CharactersMusicPlaylist>(d => d.MasterId)
                    .HasConstraintName("master_id_foreign_key_4");
            });

            modelBuilder.Entity<CharactersNote>(entity =>
            {
                entity.HasKey(e => new
                    {
                        e.MasterId, e.NoteId
                    })
                    .HasName("PRIMARY")
                    .HasAnnotation("MySql:IndexPrefixLength",
                        new[]
                        {
                            0, 0
                        });

                entity.ToTable("characters_notes");

                entity.HasCharSet("utf8").UseCollation("utf8_general_ci");

                entity.Property(e => e.MasterId).HasColumnType("int(11) unsigned").HasColumnName("master_id");

                entity.Property(e => e.NoteId).HasColumnType("tinyint(3) unsigned").HasColumnName("note_id");

                entity.Property(e => e.Colour).HasColumnType("tinyint(3) unsigned").HasColumnName("colour");

                entity.Property(e => e.Text).HasMaxLength(50).HasColumnName("text").HasDefaultValueSql("''");

                entity.HasOne(d => d.Master).WithMany(p => p.CharactersNotes).HasForeignKey(d => d.MasterId).HasConstraintName("master_id_foreign_key_6");
            });

            modelBuilder.Entity<CharactersOffence>(entity =>
            {
                entity.ToTable("characters_offences");

                entity.HasCharSet("latin1").UseCollation("latin1_swedish_ci");

                entity.HasIndex(e => e.MasterId, "master_id_foreign_key_5");

                entity.HasIndex(e => e.ModeratorId, "moderator_id_foreign_key");

                entity.Property(e => e.Id).HasColumnType("int(11) unsigned").HasColumnName("id");

                entity.Property(e => e.AppealId).HasColumnType("int(11) unsigned").HasColumnName("appeal_id");

                entity.Property(e => e.Date).HasColumnType("datetime").HasColumnName("date");

                entity.Property(e => e.ExpireDate).HasColumnType("datetime").HasColumnName("expire_date");

                entity.Property(e => e.Expired).HasColumnType("tinyint(3) unsigned").HasColumnName("expired");

                entity.Property(e => e.MasterId).HasColumnType("int(11) unsigned").HasColumnName("master_id");

                entity.Property(e => e.ModeratorId).HasColumnType("int(10) unsigned").HasColumnName("moderator_id");

                entity.Property(e => e.OffenceType).HasColumnType("tinyint(4) unsigned").HasColumnName("offence_type");

                entity.Property(e => e.Reason).HasColumnType("text").HasColumnName("reason");

                entity.HasOne(d => d.Master)
                    .WithMany(p => p.CharactersOffenceMasters)
                    .HasForeignKey(d => d.MasterId)
                    .HasConstraintName("master_id_foreign_key_5");

                entity.HasOne(d => d.Moderator)
                    .WithMany(p => p.CharactersOffenceModerators)
                    .HasForeignKey(d => d.ModeratorId)
                    .HasConstraintName("moderator_id_foreign_key");
            });

            modelBuilder.Entity<CharactersPermission>(entity =>
            {
                entity.HasKey(e => new
                    {
                        e.MasterId, e.Permission
                    })
                    .HasName("PRIMARY")
                    .HasAnnotation("MySql:IndexPrefixLength",
                        new[]
                        {
                            0, 0
                        });

                entity.ToTable("characters_permissions");

                entity.HasCharSet("latin1").UseCollation("latin1_swedish_ci");

                entity.Property(e => e.MasterId).HasColumnName("master_id");

                entity.Property(e => e.Permission)
                    .HasColumnType("enum('SystemAdministrator','GameAdministrator','GameModerator','Donator')")
                    .HasColumnName("permission")
                    .UseCollation("utf8mb4_0900_ai_ci")
                    .HasCharSet("utf8mb4");

                entity.HasOne(d => d.Master).WithMany(p => p.CharactersPermissions).HasForeignKey(d => d.MasterId).HasConstraintName("master_id_foreign_key_7");
            });

            modelBuilder.Entity<CharactersPreference>(entity =>
            {
                entity.HasKey(e => e.MasterId).HasName("PRIMARY");

                entity.ToTable("characters_preferences");

                entity.HasCharSet("latin1").UseCollation("latin1_swedish_ci");

                entity.Property(e => e.MasterId).HasColumnType("int(11) unsigned").ValueGeneratedNever().HasColumnName("master_id");

                entity.Property(e => e.AcceptAid).HasColumnType("tinyint(3) unsigned").HasColumnName("accept_aid").HasDefaultValueSql("'1'");

                entity.Property(e => e.AssistFilter).HasColumnType("tinyint(3) unsigned").HasColumnName("assist_filter");

                entity.Property(e => e.AttackStyleOptionId).HasColumnType("tinyint(3) unsigned").HasColumnName("attack_style_option_id");

                entity.Property(e => e.AutoRetaliating).HasColumnType("tinyint(3) unsigned").HasColumnName("auto_retaliating").HasDefaultValueSql("'1'");

                entity.Property(e => e.BankTabs).HasMaxLength(32).HasColumnName("bank_tabs").HasDefaultValueSql("'0,0,0,0,0,0,0,0'");

                entity.Property(e => e.Bankx).HasColumnType("int(11)").HasColumnName("bankx").HasDefaultValueSql("'1'");

                entity.Property(e => e.CcLastEntered).HasMaxLength(20).HasColumnName("cc_last_entered").HasDefaultValueSql("''");

                entity.Property(e => e.ChatEffects).HasColumnType("tinyint(3) unsigned").HasColumnName("chat_effects").HasDefaultValueSql("'1'");

                entity.Property(e => e.ClanFilter).HasColumnType("tinyint(3) unsigned").HasColumnName("clan_filter");

                entity.Property(e => e.DefensiveCasting).HasColumnType("tinyint(3) unsigned").HasColumnName("defensive_casting");

                entity.Property(e => e.FcLastEntered).HasMaxLength(12).HasColumnName("fc_last_entered").HasDefaultValueSql("''");

                entity.Property(e => e.FcLootShare).HasColumnType("tinyint(3) unsigned").HasColumnName("fc_loot_share");

                entity.Property(e => e.FcName).HasMaxLength(20).HasColumnName("fc_name").HasDefaultValueSql("''");

                entity.Property(e => e.FcRankEnter).HasColumnType("tinyint(3)").HasColumnName("fc_rank_enter").HasDefaultValueSql("'-1'");

                entity.Property(e => e.FcRankKick).HasColumnType("tinyint(3)").HasColumnName("fc_rank_kick").HasDefaultValueSql("'7'");

                entity.Property(e => e.FcRankLoot).HasColumnType("tinyint(3)").HasColumnName("fc_rank_loot").HasDefaultValueSql("'-2'");

                entity.Property(e => e.FcRankTalk).HasColumnType("tinyint(3)").HasColumnName("fc_rank_talk").HasDefaultValueSql("'-1'");

                entity.Property(e => e.FilterProfanity).HasColumnType("tinyint(3) unsigned").HasColumnName("filter_profanity");

                entity.Property(e => e.FriendsFilter).HasColumnType("tinyint(3) unsigned").HasColumnName("friends_filter");

                entity.Property(e => e.GameFilter).HasColumnType("tinyint(3) unsigned").HasColumnName("game_filter");

                entity.Property(e => e.GuestCcLastEntered).HasMaxLength(20).HasColumnName("guest_cc_last_entered").HasDefaultValueSql("''");

                entity.Property(e => e.HideCombatSpells).HasColumnType("tinyint(3) unsigned").HasColumnName("hide_combat_spells");

                entity.Property(e => e.HideMiscSpells).HasColumnType("tinyint(3) unsigned").HasColumnName("hide_misc_spells");

                entity.Property(e => e.HideSkillSpells).HasColumnType("tinyint(3) unsigned").HasColumnName("hide_skill_spells");

                entity.Property(e => e.HideTeleportSpells).HasColumnType("tinyint(3) unsigned").HasColumnName("hide_teleport_spells");

                entity.Property(e => e.MagicBook).HasColumnType("smallint(6) unsigned").HasColumnName("magic_book").HasDefaultValueSql("'192'");

                entity.Property(e => e.MoneyPouchDisplay).HasColumnType("tinyint(3) unsigned").HasColumnName("money_pouch_display").HasDefaultValueSql("'1'");

                entity.Property(e => e.PmAvailability).HasColumnType("tinyint(3) unsigned").HasColumnName("pm_availability");

                entity.Property(e => e.PrayerBook).HasColumnType("tinyint(3) unsigned").HasColumnName("prayer_book");

                entity.Property(e => e.PublicFilter).HasColumnType("tinyint(3) unsigned").HasColumnName("public_filter");

                entity.Property(e => e.QuickPrayers).HasMaxLength(255).HasColumnName("quick_prayers").HasDefaultValueSql("'0'");

                entity.Property(e => e.RightClickReporting).HasColumnType("tinyint(3) unsigned").HasColumnName("right_click_reporting");

                entity.Property(e => e.Running).HasColumnType("tinyint(3) unsigned").HasColumnName("running");

                entity.Property(e => e.SingleMouse).HasColumnType("tinyint(3) unsigned").HasColumnName("single_mouse");

                entity.Property(e => e.SplitChat).HasColumnType("tinyint(3) unsigned").HasColumnName("split_chat");

                entity.Property(e => e.SumLeftClickOption).HasColumnType("tinyint(3) unsigned").HasColumnName("sum_left_click_option");

                entity.Property(e => e.TradeFilter).HasColumnType("tinyint(3) unsigned").HasColumnName("trade_filter");

                entity.Property(e => e.XpCounterDisplay).HasColumnType("tinyint(3) unsigned").HasColumnName("xp_counter_display").HasDefaultValueSql("'1'");

                entity.Property(e => e.XpCounterPopup).HasColumnType("tinyint(3) unsigned").HasColumnName("xp_counter_popup").HasDefaultValueSql("'1'");
            });

            modelBuilder.Entity<CharactersQuest>(entity =>
            {
                entity.HasKey(e => e.MasterId).HasName("PRIMARY");

                entity.ToTable("characters_quests");

                entity.HasCharSet("latin1").UseCollation("latin1_swedish_ci");

                entity.HasIndex(e => e.QuestId, "quest_id_foreign_key");

                entity.Property(e => e.MasterId).HasColumnType("int(11) unsigned").ValueGeneratedNever().HasColumnName("master_id");

                entity.Property(e => e.QuestId).HasColumnType("smallint(6) unsigned").HasColumnName("quest_id");

                entity.Property(e => e.Stage).HasColumnType("smallint(6) unsigned").HasColumnName("stage");

                entity.Property(e => e.Status).HasColumnType("tinyint(3) unsigned").HasColumnName("status");

                entity.HasOne(d => d.Master)
                    .WithOne(p => p.CharactersQuest)
                    .HasForeignKey<CharactersQuest>(d => d.MasterId)
                    .HasConstraintName("master_id_foreign_key_8");

                entity.HasOne(d => d.Quest)
                    .WithMany(p => p.CharactersQuests)
                    .HasForeignKey(d => d.QuestId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("quest_id_foreign_key");
            });

            modelBuilder.Entity<CharactersReport>(entity =>
            {
                entity.ToTable("characters_reports");

                entity.HasCharSet("latin1").UseCollation("latin1_swedish_ci");

                entity.HasIndex(e => e.ReportedId, "reported_id_foreign_key");

                entity.HasIndex(e => e.ReporterId, "reporter_id_foreign_key");

                entity.Property(e => e.Id).HasColumnType("int(11) unsigned").HasColumnName("id");

                entity.Property(e => e.Date).HasColumnType("datetime").HasColumnName("date");

                entity.Property(e => e.ReportedId).HasColumnType("int(11) unsigned").HasColumnName("reported_id");

                entity.Property(e => e.ReporterId).HasColumnType("int(11) unsigned").HasColumnName("reporter_id");

                entity.Property(e => e.Type).HasColumnType("tinyint(4) unsigned").HasColumnName("type");

                entity.HasOne(d => d.Reported)
                    .WithMany(p => p.CharactersReportReporteds)
                    .HasForeignKey(d => d.ReportedId)
                    .HasConstraintName("reported_id_foreign_key");

                entity.HasOne(d => d.Reporter)
                    .WithMany(p => p.CharactersReportReporters)
                    .HasForeignKey(d => d.ReporterId)
                    .HasConstraintName("reporter_id_foreign_key");
            });

            modelBuilder.Entity<CharactersReward>(entity =>
            {
                entity.ToTable("characters_rewards");

                entity.HasCharSet("latin1").UseCollation("latin1_swedish_ci");

                entity.HasIndex(e => e.MasterId, "master_id_foreign_key_10");

                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.Count).HasColumnName("count").HasDefaultValueSql("'1'");

                entity.Property(e => e.ExtraData).HasColumnName("extra_data").UseCollation("utf8mb4_0900_ai_ci").HasCharSet("utf8mb4");

                entity.Property(e => e.ItemId).HasColumnName("item_id");

                entity.Property(e => e.Loaded).HasColumnName("loaded");

                entity.Property(e => e.MasterId).HasColumnName("master_id");

                entity.HasOne(d => d.Master).WithMany(p => p.CharactersRewards).HasForeignKey(d => d.MasterId).HasConstraintName("master_id_foreign_key_10");
            });

            modelBuilder.Entity<CharactersSlayerTask>(entity =>
            {
                entity.HasKey(e => e.MasterId).HasName("PRIMARY");

                entity.ToTable("characters_slayer_tasks");

                entity.HasCharSet("latin1").UseCollation("latin1_swedish_ci");

                entity.HasIndex(e => e.SlayerTaskId, "slayer_task_id_foreign_key");

                entity.Property(e => e.MasterId).HasColumnType("int(11) unsigned").ValueGeneratedNever().HasColumnName("master_id");

                entity.Property(e => e.KillCount).HasColumnType("int(11) unsigned").HasColumnName("kill_count");

                entity.Property(e => e.SlayerTaskId).HasColumnType("smallint(6) unsigned").HasColumnName("slayer_task_id");

                entity.HasOne(d => d.Master)
                    .WithOne(p => p.CharactersSlayerTask)
                    .HasForeignKey<CharactersSlayerTask>(d => d.MasterId)
                    .HasConstraintName("master_id_foreign_key_16");

                entity.HasOne(d => d.SlayerTask)
                    .WithMany(p => p.CharactersSlayerTasks)
                    .HasForeignKey(d => d.SlayerTaskId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("slayer_task_id_foreign_key");
            });

            modelBuilder.Entity<CharactersState>(entity =>
            {
                entity.HasKey(e => new
                    {
                        e.MasterId, e.StateId
                    })
                    .HasName("PRIMARY")
                    .HasAnnotation("MySql:IndexPrefixLength",
                        new[]
                        {
                            0, 0
                        });

                entity.ToTable("characters_states");

                entity.HasCharSet("utf8").UseCollation("utf8_general_ci");

                entity.Property(e => e.MasterId).HasColumnType("int(11) unsigned").HasColumnName("master_id");

                entity.Property(e => e.StateId).HasColumnType("int(11)").HasColumnName("state_id");

                entity.Property(e => e.TicksLeft).HasColumnType("int(11)").HasColumnName("ticks_left");

                entity.HasOne(d => d.Master).WithMany(p => p.CharactersStates).HasForeignKey(d => d.MasterId).HasConstraintName("master_id_foreign_key_17");
            });

            modelBuilder.Entity<CharactersStatistic>(entity =>
            {
                entity.HasKey(e => e.MasterId).HasName("PRIMARY");

                entity.ToTable("characters_statistics");

                entity.HasCharSet("latin1").UseCollation("latin1_swedish_ci");

                entity.Property(e => e.MasterId).HasColumnType("int(11) unsigned").ValueGeneratedNever().HasColumnName("master_id");

                entity.Property(e => e.AgilityExp).HasColumnType("int(11)").HasColumnName("agility_exp");

                entity.Property(e => e.AgilityLevel).HasColumnType("tinyint(3) unsigned").HasColumnName("agility_level").HasDefaultValueSql("'1'");

                entity.Property(e => e.AttackExp).HasColumnType("int(11)").HasColumnName("attack_exp");

                entity.Property(e => e.AttackLevel).HasColumnType("tinyint(3) unsigned").HasColumnName("attack_level").HasDefaultValueSql("'1'");

                entity.Property(e => e.ConstitutionExp).HasColumnType("int(11)").HasColumnName("constitution_exp").HasDefaultValueSql("'1154'");

                entity.Property(e => e.ConstitutionLevel).HasColumnType("tinyint(3) unsigned").HasColumnName("constitution_level").HasDefaultValueSql("'10'");

                entity.Property(e => e.ConstructionExp).HasColumnType("int(11)").HasColumnName("construction_exp");

                entity.Property(e => e.ConstructionLevel).HasColumnType("tinyint(3) unsigned").HasColumnName("construction_level").HasDefaultValueSql("'1'");

                entity.Property(e => e.CookingExp).HasColumnType("int(11)").HasColumnName("cooking_exp");

                entity.Property(e => e.CookingLevel).HasColumnType("tinyint(3) unsigned").HasColumnName("cooking_level").HasDefaultValueSql("'1'");

                entity.Property(e => e.CraftingExp).HasColumnType("int(11)").HasColumnName("crafting_exp");

                entity.Property(e => e.CraftingLevel).HasColumnType("tinyint(3) unsigned").HasColumnName("crafting_level").HasDefaultValueSql("'1'");

                entity.Property(e => e.DefenceExp).HasColumnType("int(11)").HasColumnName("defence_exp");

                entity.Property(e => e.DefenceLevel).HasColumnType("tinyint(3) unsigned").HasColumnName("defence_level").HasDefaultValueSql("'1'");

                entity.Property(e => e.DungeoneeringExp).HasColumnType("int(11)").HasColumnName("dungeoneering_exp");

                entity.Property(e => e.DungeoneeringLevel).HasColumnType("tinyint(3) unsigned").HasColumnName("dungeoneering_level").HasDefaultValueSql("'1'");

                entity.Property(e => e.EnabledXpCounters).HasMaxLength(12).HasColumnName("enabled_xp_counters").HasDefaultValueSql("'1,0,0'");

                entity.Property(e => e.FarmingExp).HasColumnType("int(11)").HasColumnName("farming_exp");

                entity.Property(e => e.FarmingLevel).HasColumnType("tinyint(3) unsigned").HasColumnName("farming_level").HasDefaultValueSql("'1'");

                entity.Property(e => e.FiremakingExp).HasColumnType("int(11)").HasColumnName("firemaking_exp");

                entity.Property(e => e.FiremakingLevel).HasColumnType("tinyint(3) unsigned").HasColumnName("firemaking_level").HasDefaultValueSql("'1'");

                entity.Property(e => e.FishingExp).HasColumnType("int(11)").HasColumnName("fishing_exp");

                entity.Property(e => e.FishingLevel).HasColumnType("tinyint(3) unsigned").HasColumnName("fishing_level").HasDefaultValueSql("'1'");

                entity.Property(e => e.FletchingExp).HasColumnType("int(11)").HasColumnName("fletching_exp");

                entity.Property(e => e.FletchingLevel).HasColumnType("tinyint(3) unsigned").HasColumnName("fletching_level").HasDefaultValueSql("'1'");

                entity.Property(e => e.HerbloreExp).HasColumnType("int(11)").HasColumnName("herblore_exp");

                entity.Property(e => e.HerbloreLevel).HasColumnType("tinyint(3) unsigned").HasColumnName("herblore_level").HasDefaultValueSql("'1'");

                entity.Property(e => e.HunterExp).HasColumnType("int(11)").HasColumnName("hunter_exp");

                entity.Property(e => e.HunterLevel).HasColumnType("tinyint(3) unsigned").HasColumnName("hunter_level").HasDefaultValueSql("'1'");

                entity.Property(e => e.LifePoints).HasColumnType("smallint(6) unsigned").HasColumnName("life_points").HasDefaultValueSql("'100'");

                entity.Property(e => e.MagicExp).HasColumnType("int(11)").HasColumnName("magic_exp");

                entity.Property(e => e.MagicLevel).HasColumnType("tinyint(3) unsigned").HasColumnName("magic_level").HasDefaultValueSql("'1'");

                entity.Property(e => e.MiningExp).HasColumnType("int(11)").HasColumnName("mining_exp");

                entity.Property(e => e.MiningLevel).HasColumnType("tinyint(3) unsigned").HasColumnName("mining_level").HasDefaultValueSql("'1'");

                entity.Property(e => e.PlayTime).HasColumnType("bigint(19) unsigned").HasColumnName("play_time");

                entity.Property(e => e.PoisonAmount).HasColumnType("smallint(6) unsigned").HasColumnName("poison_amount");

                entity.Property(e => e.PrayerExp).HasColumnType("int(11)").HasColumnName("prayer_exp");

                entity.Property(e => e.PrayerLevel).HasColumnType("tinyint(3) unsigned").HasColumnName("prayer_level").HasDefaultValueSql("'1'");

                entity.Property(e => e.PrayerPoints).HasColumnType("smallint(6) unsigned").HasColumnName("prayer_points").HasDefaultValueSql("'100'");

                entity.Property(e => e.RangeExp).HasColumnType("int(11)").HasColumnName("range_exp");

                entity.Property(e => e.RangeLevel).HasColumnType("tinyint(3) unsigned").HasColumnName("range_level").HasDefaultValueSql("'1'");

                entity.Property(e => e.RunEnergy).HasColumnType("tinyint(6) unsigned").HasColumnName("run_energy").HasDefaultValueSql("'100'");

                entity.Property(e => e.RunecraftingExp).HasColumnType("int(11)").HasColumnName("runecrafting_exp");

                entity.Property(e => e.RunecraftingLevel).HasColumnType("tinyint(3) unsigned").HasColumnName("runecrafting_level").HasDefaultValueSql("'1'");

                entity.Property(e => e.SlayerExp).HasColumnType("int(11)").HasColumnName("slayer_exp");

                entity.Property(e => e.SlayerLevel).HasColumnType("tinyint(3) unsigned").HasColumnName("slayer_level").HasDefaultValueSql("'1'");

                entity.Property(e => e.SmithingExp).HasColumnType("int(11)").HasColumnName("smithing_exp");

                entity.Property(e => e.SmithingLevel).HasColumnType("tinyint(3) unsigned").HasColumnName("smithing_level").HasDefaultValueSql("'1'");

                entity.Property(e => e.SpecialEnergy).HasColumnType("smallint(6) unsigned").HasColumnName("special_energy").HasDefaultValueSql("'1000'");

                entity.Property(e => e.StrengthExp).HasColumnType("int(11)").HasColumnName("strength_exp");

                entity.Property(e => e.StrengthLevel).HasColumnType("tinyint(3) unsigned").HasColumnName("strength_level").HasDefaultValueSql("'1'");

                entity.Property(e => e.SummoningExp).HasColumnType("int(11)").HasColumnName("summoning_exp");

                entity.Property(e => e.SummoningLevel).HasColumnType("tinyint(3) unsigned").HasColumnName("summoning_level").HasDefaultValueSql("'1'");

                entity.Property(e => e.TargetSkillExperiences)
                    .HasMaxLength(312)
                    .HasColumnName("target_skill_experiences")
                    .HasDefaultValueSql("'-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1'");

                entity.Property(e => e.TargetSkillLevels)
                    .HasMaxLength(255)
                    .HasColumnName("target_skill_levels")
                    .HasDefaultValueSql("'-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1'");

                entity.Property(e => e.ThievingExp).HasColumnType("int(11)").HasColumnName("thieving_exp");

                entity.Property(e => e.ThievingLevel).HasColumnType("tinyint(3) unsigned").HasColumnName("thieving_level").HasDefaultValueSql("'1'");

                entity.Property(e => e.TrackedXpCounters).HasMaxLength(12).HasColumnName("tracked_xp_counters").HasDefaultValueSql("'30,0,0'");

                entity.Property(e => e.WoodcuttingExp).HasColumnType("int(11)").HasColumnName("woodcutting_exp");

                entity.Property(e => e.WoodcuttingLevel).HasColumnType("tinyint(3) unsigned").HasColumnName("woodcutting_level").HasDefaultValueSql("'1'");

                entity.Property(e => e.XpCounters).HasMaxLength(72).HasColumnName("xp_counters").HasDefaultValueSql("'0,0,0'");

                entity.HasOne(d => d.Master)
                    .WithOne(p => p.CharactersStatistic)
                    .HasForeignKey<CharactersStatistic>(d => d.MasterId)
                    .HasConstraintName("master_id_foreign_key_11");
            });

            modelBuilder.Entity<CharactersTicket>(entity =>
            {
                entity.ToTable("characters_tickets", t => t.HasComment("Player System"));

                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.Completed).HasColumnName("completed");

                entity.Property(e => e.MasterId).HasColumnName("master_id");

                entity.Property(e => e.ModeratorId).HasColumnName("moderator_id");

                entity.Property(e => e.ResponseText).HasColumnName("response_text").UseCollation("utf8mb4_0900_ai_ci").HasCharSet("utf8mb4");

                entity.Property(e => e.TicketLastchange)
                    .HasColumnType("timestamp")
                    .ValueGeneratedOnAddOrUpdate()
                    .HasColumnName("ticket_lastchange")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.TicketText).HasColumnName("ticket_text").UseCollation("utf8mb4_0900_ai_ci").HasCharSet("utf8mb4");
            });

            modelBuilder.Entity<CharactersVariable>(entity =>
            {
                entity.HasKey(e => new
                    {
                        e.MasterId, e.Variable
                    })
                    .HasName("PRIMARY")
                    .HasAnnotation("MySql:IndexPrefixLength",
                        new[]
                        {
                            0, 0
                        });

                entity.ToTable("characters_variables");

                entity.HasCharSet("utf8").UseCollation("utf8_general_ci");

                entity.Property(e => e.MasterId).HasColumnName("master_id");

                entity.Property(e => e.Variable).HasColumnName("variable");

                entity.Property(e => e.IntValue).HasColumnName("int_value");

                entity.Property(e => e.StringValue)
                    .HasMaxLength(255)
                    .HasColumnName("string_value")
                    .HasDefaultValueSql("''")
                    .UseCollation("utf8mb4_0900_ai_ci")
                    .HasCharSet("utf8mb4");
            });

            modelBuilder.Entity<CharacterscreateinfoItem>(entity =>
            {
                entity.HasKey(e => new
                    {
                        e.ItemId, e.ContainerType
                    })
                    .HasName("PRIMARY")
                    .HasAnnotation("MySql:IndexPrefixLength",
                        new[]
                        {
                            0, 0
                        });

                entity.ToTable("characterscreateinfo_items");

                entity.HasCharSet("utf8").UseCollation("utf8_general_ci");

                entity.Property(e => e.ItemId).HasColumnType("smallint(6) unsigned").HasColumnName("item_id");

                entity.Property(e => e.ContainerType).HasColumnType("tinyint(3) unsigned").HasColumnName("container_type").HasDefaultValueSql("'2'");

                entity.Property(e => e.Count).HasColumnType("int(11)").HasColumnName("count").HasDefaultValueSql("'1'");
            });

            modelBuilder.Entity<Clan>(entity =>
            {
                entity.ToTable("clans");

                entity.HasCharSet("utf8").UseCollation("utf8_general_ci");

                entity.Property(e => e.Id).HasColumnType("int(11) unsigned").HasColumnName("id");

                entity.Property(e => e.CreationDate).HasColumnType("datetime").HasColumnName("creation_date");

                entity.Property(e => e.Name).HasMaxLength(20).HasColumnName("name").HasDefaultValueSql("''");

                entity.HasMany(d => d.Masters)
                    .WithMany(p => p.Clans)
                    .UsingEntity<Dictionary<string, object>>("ClansBan",
                        l => l.HasOne<Character>().WithMany().HasForeignKey("MasterId").HasConstraintName("master_id_foreign_key_18"),
                        r => r.HasOne<Clan>().WithMany().HasForeignKey("ClanId").HasConstraintName("clan_id_foreign_key_3"),
                        j =>
                        {
                            j.HasKey("ClanId", "MasterId")
                                .HasName("PRIMARY")
                                .HasAnnotation("MySql:IndexPrefixLength",
                                    new[]
                                    {
                                        0, 0
                                    });

                            j.ToTable("clans_bans").HasCharSet("utf8").UseCollation("utf8_general_ci");

                            j.HasIndex(new[]
                                {
                                    "MasterId"
                                },
                                "master_id_foreign_key_18");

                            j.IndexerProperty<uint>("ClanId").HasColumnType("int(11) unsigned").HasColumnName("clan_id");

                            j.IndexerProperty<uint>("MasterId").HasColumnType("int(11) unsigned").HasColumnName("master_id");
                        });
            });

            modelBuilder.Entity<ClansMember>(entity =>
            {
                entity.HasKey(e => e.MasterId).HasName("PRIMARY");

                entity.ToTable("clans_members");

                entity.HasCharSet("utf8").UseCollation("utf8_general_ci");

                entity.HasIndex(e => e.ClanId, "clan_id_foreign_key_46");

                entity.HasIndex(e => e.RecruiterId, "recruiter_id_foreign_key");

                entity.Property(e => e.MasterId).ValueGeneratedNever().HasColumnName("master_id");

                entity.Property(e => e.ClanId).HasColumnName("clan_id");

                entity.Property(e => e.Rank).HasColumnName("rank");

                entity.Property(e => e.RecruiterId).HasColumnName("recruiter_id");

                entity.HasOne(d => d.Clan).WithMany(p => p.ClansMembers).HasForeignKey(d => d.ClanId).HasConstraintName("clan_id_foreign_key_46");

                entity.HasOne(d => d.Master)
                    .WithOne(p => p.ClansMemberMaster)
                    .HasForeignKey<ClansMember>(d => d.MasterId)
                    .HasConstraintName("master_id_foreign_key_19");

                entity.HasOne(d => d.Recruiter)
                    .WithMany(p => p.ClansMemberRecruiters)
                    .HasForeignKey(d => d.RecruiterId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("recruiter_id_foreign_key");
            });

            modelBuilder.Entity<ClansSetting>(entity =>
            {
                entity.HasKey(e => e.ClanId).HasName("PRIMARY");

                entity.ToTable("clans_settings");

                entity.HasCharSet("utf8").UseCollation("utf8_general_ci");

                entity.Property(e => e.ClanId).HasColumnType("int(11) unsigned").ValueGeneratedNever().HasColumnName("clan_id");

                entity.Property(e => e.ClanTime).HasColumnType("tinyint(3) unsigned").HasColumnName("clan_time");

                entity.Property(e => e.MottifBottom).HasColumnType("tinyint(3) unsigned").HasColumnName("mottif_bottom");

                entity.Property(e => e.MottifColourLeftTop).HasColumnType("smallint(6)").HasColumnName("mottif_colour_left_top").HasDefaultValueSql("'-1'");

                entity.Property(e => e.MottifColourRightBottom)
                    .HasColumnType("smallint(6)")
                    .HasColumnName("mottif_colour_right_bottom")
                    .HasDefaultValueSql("'-1'");

                entity.Property(e => e.MottifTop).HasColumnType("tinyint(3) unsigned").HasColumnName("mottif_top");

                entity.Property(e => e.Motto).HasMaxLength(80).HasColumnName("motto");

                entity.Property(e => e.NationalFlag).HasColumnType("tinyint(3) unsigned").HasColumnName("national_flag");

                entity.Property(e => e.PrimaryClanColour).HasColumnType("smallint(6)").HasColumnName("primary_clan_colour").HasDefaultValueSql("'-1'");

                entity.Property(e => e.RankToEnterCc).HasColumnType("tinyint(3)").HasColumnName("rank_to_enter_cc").HasDefaultValueSql("'-1'");

                entity.Property(e => e.RankToKick).HasColumnType("tinyint(3)").HasColumnName("rank_to_kick").HasDefaultValueSql("'100'");

                entity.Property(e => e.RankToTalk).HasColumnType("tinyint(3)").HasColumnName("rank_to_talk").HasDefaultValueSql("'-1'");

                entity.Property(e => e.Recruiting).HasColumnType("tinyint(3) unsigned").HasColumnName("recruiting").HasDefaultValueSql("'1'");

                entity.Property(e => e.SecondaryClanColour).HasColumnType("smallint(6)").HasColumnName("secondary_clan_colour").HasDefaultValueSql("'-1'");

                entity.Property(e => e.ThreadId).HasMaxLength(128).HasColumnName("thread_id");

                entity.Property(e => e.TimeZone).HasColumnType("smallint(6)").HasColumnName("time_zone").HasDefaultValueSql("'-1'");

                entity.Property(e => e.WorldId).HasColumnType("smallint(6) unsigned").HasColumnName("world_id").HasDefaultValueSql("'1'");

                entity.HasOne(d => d.Clan).WithOne(p => p.ClansSetting).HasForeignKey<ClansSetting>(d => d.ClanId).HasConstraintName("clan_id_foreign_key_6");
            });

            modelBuilder.Entity<Configuration>(entity =>
            {
                entity.HasKey(e => e.Name).HasName("PRIMARY");

                entity.ToTable("configurations");

                entity.HasCharSet("latin1").UseCollation("latin1_swedish_ci");

                entity.Property(e => e.Name).HasColumnName("name").HasDefaultValueSql("''");

                entity.Property(e => e.Type).HasMaxLength(7).HasColumnName("type");

                entity.Property(e => e.Value).HasMaxLength(255).HasColumnName("value");
            });

            modelBuilder.Entity<Efmigrationshistory>(entity =>
            {
                entity.HasKey(e => e.MigrationId).HasName("PRIMARY");

                entity.ToTable("__efmigrationshistory");

                entity.Property(e => e.MigrationId).HasMaxLength(95);

                entity.Property(e => e.ProductVersion).HasMaxLength(32);
            });

            modelBuilder.Entity<EquipmentBonuse>(entity =>
            {
                entity.HasKey(e => e.ItemId).HasName("PRIMARY");

                entity.ToTable("equipment_bonuses");

                entity.HasCharSet("latin1").UseCollation("latin1_swedish_ci");

                entity.Property(e => e.ItemId).HasColumnType("smallint(6) unsigned").ValueGeneratedNever().HasColumnName("item_id");

                entity.Property(e => e.AbsorbMagic).HasColumnType("int(32)").HasColumnName("absorb_magic");

                entity.Property(e => e.AbsorbMelee).HasColumnType("int(32)").HasColumnName("absorb_melee");

                entity.Property(e => e.AbsorbRange).HasColumnType("int(32)").HasColumnName("absorb_range");

                entity.Property(e => e.AttackCrush).HasColumnType("int(32)").HasColumnName("attack_crush");

                entity.Property(e => e.AttackMagic).HasColumnType("int(32)").HasColumnName("attack_magic");

                entity.Property(e => e.AttackRanged).HasColumnType("int(32)").HasColumnName("attack_ranged");

                entity.Property(e => e.AttackSlash).HasColumnType("int(32)").HasColumnName("attack_slash");

                entity.Property(e => e.AttackStab).HasColumnType("int(32)").HasColumnName("attack_stab");

                entity.Property(e => e.DefenceCrush).HasColumnType("int(32)").HasColumnName("defence_crush");

                entity.Property(e => e.DefenceMagic).HasColumnType("int(32)").HasColumnName("defence_magic");

                entity.Property(e => e.DefenceRanged).HasColumnType("int(32)").HasColumnName("defence_ranged");

                entity.Property(e => e.DefenceSlash).HasColumnType("int(32)").HasColumnName("defence_slash");

                entity.Property(e => e.DefenceStab).HasColumnType("int(32)").HasColumnName("defence_stab");

                entity.Property(e => e.DefenceSummoning).HasColumnType("int(32)").HasColumnName("defence_summoning");

                entity.Property(e => e.MagicDamage).HasColumnType("int(32)").HasColumnName("magic_damage");

                entity.Property(e => e.Prayer).HasColumnType("int(32)").HasColumnName("prayer");

                entity.Property(e => e.RangedStrength).HasColumnType("int(32)").HasColumnName("ranged_strength");

                entity.Property(e => e.Strength).HasColumnType("int(32)").HasColumnName("strength");
            });

            modelBuilder.Entity<EquipmentDefinition>(entity =>
            {
                entity.ToTable("equipment_definitions");

                entity.HasCharSet("latin1").UseCollation("latin1_swedish_ci");

                entity.Property(e => e.Id).ValueGeneratedNever().HasColumnName("id");

                entity.Property(e => e.AttackDistance).HasColumnName("attack_distance").HasDefaultValueSql("'1'");

                entity.Property(e => e.Attackanim1).HasColumnName("attackanim1").HasDefaultValueSql("'-1'");

                entity.Property(e => e.Attackanim2).HasColumnName("attackanim2").HasDefaultValueSql("'-1'");

                entity.Property(e => e.Attackanim3).HasColumnName("attackanim3").HasDefaultValueSql("'-1'");

                entity.Property(e => e.Attackanim4).HasColumnName("attackanim4").HasDefaultValueSql("'-1'");

                entity.Property(e => e.Attackgfx1).HasColumnName("attackgfx1").HasDefaultValueSql("'-1'");

                entity.Property(e => e.Attackgfx2).HasColumnName("attackgfx2").HasDefaultValueSql("'-1'");

                entity.Property(e => e.Attackgfx3).HasColumnName("attackgfx3").HasDefaultValueSql("'-1'");

                entity.Property(e => e.Attackgfx4).HasColumnName("attackgfx4").HasDefaultValueSql("'-1'");

                entity.Property(e => e.DefenceAnim).HasColumnName("defence_anim").HasDefaultValueSql("'-1'");

                entity.Property(e => e.Fullbody)
                    .HasColumnType("enum('false','true')")
                    .HasColumnName("fullbody")
                    .UseCollation("utf8mb4_0900_ai_ci")
                    .HasCharSet("utf8mb4");

                entity.Property(e => e.Fullhat)
                    .HasColumnType("enum('false','true')")
                    .HasColumnName("fullhat")
                    .UseCollation("utf8mb4_0900_ai_ci")
                    .HasCharSet("utf8mb4");

                entity.Property(e => e.Fullmask)
                    .HasColumnType("enum('false','true')")
                    .HasColumnName("fullmask")
                    .UseCollation("utf8mb4_0900_ai_ci")
                    .HasCharSet("utf8mb4");

                entity.Property(e => e.Name)
                    .HasMaxLength(50)
                    .HasColumnName("name")
                    .HasDefaultValueSql("''")
                    .HasComment("This field is updated automatically by the server.");
            });

            modelBuilder.Entity<GameEvent>(entity =>
            {
                entity.ToTable("game_events");

                entity.HasCharSet("latin1").UseCollation("latin1_swedish_ci");

                entity.Property(e => e.Id).HasColumnType("smallint(6) unsigned").HasColumnName("id");

                entity.Property(e => e.EndTime).HasColumnType("datetime").HasColumnName("end_time");

                entity.Property(e => e.Name).HasMaxLength(100).HasColumnName("name");

                entity.Property(e => e.StartTime).HasColumnType("datetime").HasColumnName("start_time");
            });

            modelBuilder.Entity<GameobjectDefinition>(entity =>
            {
                entity.HasKey(e => e.GameobjectId).HasName("PRIMARY");

                entity.ToTable("gameobject_definitions");

                entity.HasCharSet("latin1").UseCollation("latin1_swedish_ci");

                entity.HasIndex(e => e.GameobjectLootId, "gameobject_loot_id_foreign_key");

                entity.Property(e => e.GameobjectId).ValueGeneratedNever().HasColumnName("gameobject_id");

                entity.Property(e => e.Examine).HasColumnType("text").HasColumnName("examine");

                entity.Property(e => e.GameobjectLootId).HasColumnName("gameobject_loot_id").HasDefaultValueSql("'0'");

                entity.Property(e => e.Name).HasMaxLength(32).HasColumnName("name").HasDefaultValueSql("'unknown'");

                entity.HasOne(d => d.GameobjectLoot)
                    .WithMany(p => p.GameobjectDefinitions)
                    .HasForeignKey(d => d.GameobjectLootId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("gameobject_loot_id_foreign_key");
            });

            modelBuilder.Entity<GameobjectLodestone>(entity =>
            {
                entity.HasKey(e => e.GameobjectId).HasName("PRIMARY");

                entity.ToTable("gameobject_lodestones");

                entity.HasCharSet("utf8").UseCollation("utf8_general_ci");

                entity.Property(e => e.GameobjectId).HasColumnType("int(11) unsigned").ValueGeneratedNever().HasColumnName("gameobject_id");

                entity.Property(e => e.ButtonId).HasColumnType("smallint(6) unsigned").HasColumnName("button_id");

                entity.Property(e => e.CoordX).HasColumnType("smallint(6) unsigned").HasColumnName("coord_x");

                entity.Property(e => e.CoordY).HasColumnType("smallint(6) unsigned").HasColumnName("coord_y");

                entity.Property(e => e.CoordZ).HasColumnType("tinyint(3) unsigned").HasColumnName("coord_z");

                entity.Property(e => e.StateId).HasColumnType("varchar(255)").HasColumnName("state_id");
            });

            modelBuilder.Entity<GameobjectLoot>(entity =>
            {
                entity.ToTable("gameobject_loot");

                entity.HasCharSet("latin1").UseCollation("latin1_swedish_ci");

                entity.Property(e => e.Id).HasColumnType("smallint(6) unsigned").HasColumnName("id");

                entity.Property(e => e.MaximumLootCount).HasColumnType("int(11) unsigned").HasColumnName("maximum_loot_count");

                entity.Property(e => e.Name).HasMaxLength(32).HasColumnName("name");

                entity.Property(e => e.RandomizeLootCount).HasColumnType("tinyint(3) unsigned").HasColumnName("randomize_loot_count").HasDefaultValueSql("'1'");

                entity.HasMany(d => d.GameobjectLootChildren)
                    .WithMany(p => p.GameobjectLootParents)
                    .UsingEntity<Dictionary<string, object>>("GameobjectLootrecursion",
                        l => l.HasOne<GameobjectLoot>()
                            .WithMany()
                            .HasForeignKey("GameobjectLootChildId")
                            .HasConstraintName("gameobject_loot_child_id_foreign_key"),
                        r => r.HasOne<GameobjectLoot>()
                            .WithMany()
                            .HasForeignKey("GameobjectLootParentId")
                            .HasConstraintName("gameobject_loot_parent_id_foreign_key"),
                        j =>
                        {
                            j.HasKey("GameobjectLootChildId", "GameobjectLootParentId")
                                .HasName("PRIMARY")
                                .HasAnnotation("MySql:IndexPrefixLength",
                                    new[]
                                    {
                                        0, 0
                                    });

                            j.ToTable("gameobject_lootrecursion").HasCharSet("utf8").UseCollation("utf8_general_ci");

                            j.HasIndex(new[]
                                {
                                    "GameobjectLootParentId"
                                },
                                "gameobject_loot_parent_id_foreign_key");

                            j.IndexerProperty<ushort>("GameobjectLootChildId").HasColumnType("smallint(6) unsigned").HasColumnName("gameobject_loot_child_id");

                            j.IndexerProperty<ushort>("GameobjectLootParentId")
                                .HasColumnType("smallint(6) unsigned")
                                .HasColumnName("gameobject_loot_parent_id");
                        });

                entity.HasMany(d => d.GameobjectLootParents)
                    .WithMany(p => p.GameobjectLootChildren)
                    .UsingEntity<Dictionary<string, object>>("GameobjectLootrecursion",
                        l => l.HasOne<GameobjectLoot>()
                            .WithMany()
                            .HasForeignKey("GameobjectLootParentId")
                            .HasConstraintName("gameobject_loot_parent_id_foreign_key"),
                        r => r.HasOne<GameobjectLoot>()
                            .WithMany()
                            .HasForeignKey("GameobjectLootChildId")
                            .HasConstraintName("gameobject_loot_child_id_foreign_key"),
                        j =>
                        {
                            j.HasKey("GameobjectLootChildId", "GameobjectLootParentId")
                                .HasName("PRIMARY")
                                .HasAnnotation("MySql:IndexPrefixLength",
                                    new[]
                                    {
                                        0, 0
                                    });

                            j.ToTable("gameobject_lootrecursion").HasCharSet("utf8").UseCollation("utf8_general_ci");

                            j.HasIndex(new[]
                                {
                                    "GameobjectLootParentId"
                                },
                                "gameobject_loot_parent_id_foreign_key");

                            j.IndexerProperty<ushort>("GameobjectLootChildId").HasColumnType("smallint(6) unsigned").HasColumnName("gameobject_loot_child_id");

                            j.IndexerProperty<ushort>("GameobjectLootParentId")
                                .HasColumnType("smallint(6) unsigned")
                                .HasColumnName("gameobject_loot_parent_id");
                        });
            });

            modelBuilder.Entity<GameobjectLootItem>(entity =>
            {
                entity.ToTable("gameobject_loot_items");

                entity.HasCharSet("latin1").UseCollation("latin1_swedish_ci");

                entity.HasIndex(e => e.GameobjectLootId, "gameobject_loot_id_foreign_key_1");

                entity.Property(e => e.Id).HasColumnType("int(11) unsigned").HasColumnName("id");

                entity.Property(e => e.Always).HasColumnType("tinyint(1) unsigned").HasColumnName("always");

                entity.Property(e => e.GameobjectLootId).HasColumnType("smallint(6) unsigned").HasColumnName("gameobject_loot_id");

                entity.Property(e => e.ItemId).HasColumnType("smallint(6) unsigned").HasColumnName("item_id");

                entity.Property(e => e.MaximumCount).HasColumnType("int(11) unsigned").HasColumnName("maximum_count").HasDefaultValueSql("'1'");

                entity.Property(e => e.MinimumCount).HasColumnType("int(11) unsigned").HasColumnName("minimum_count");

                entity.Property(e => e.Probability).HasPrecision(8, 3).HasColumnName("probability");

                entity.HasOne(d => d.GameobjectLoot)
                    .WithMany(p => p.GameobjectLootItems)
                    .HasForeignKey(d => d.GameobjectLootId)
                    .HasConstraintName("gameobject_loot_id_foreign_key_1");
            });

            modelBuilder.Entity<GameobjectSpawn>(entity =>
            {
                entity.HasKey(e => e.SpawnId).HasName("PRIMARY");

                entity.ToTable("gameobject_spawns");

                entity.HasCharSet("latin1").UseCollation("latin1_swedish_ci");

                entity.Property(e => e.SpawnId).HasColumnType("int(11)").HasColumnName("spawn_id");

                entity.Property(e => e.CoordX).HasColumnType("smallint(6)").HasColumnName("coord_x").HasDefaultValueSql("'3200'");

                entity.Property(e => e.CoordY).HasColumnType("smallint(6)").HasColumnName("coord_y").HasDefaultValueSql("'3200'");

                entity.Property(e => e.CoordZ).HasColumnType("smallint(6) unsigned").HasColumnName("coord_z");

                entity.Property(e => e.Face).HasColumnType("smallint(6)").HasColumnName("face");

                entity.Property(e => e.GameobjectId).HasColumnType("int(8) unsigned").HasColumnName("gameobject_id");

                entity.Property(e => e.Type).HasColumnType("smallint(11)").HasColumnName("type").HasDefaultValueSql("'10'");
            });

            modelBuilder.Entity<ItemCombine>(entity =>
            {
                entity.HasKey(e => e.ReqItemId1).HasName("PRIMARY");

                entity.ToTable("item_combines");

                entity.HasCharSet("utf8").UseCollation("utf8_general_ci");

                entity.Property(e => e.ReqItemId1).HasColumnType("smallint(6) unsigned").ValueGeneratedNever().HasColumnName("req_item_id_1");

                entity.Property(e => e.AnimId).HasColumnType("smallint(6) unsigned").HasColumnName("anim_id");

                entity.Property(e => e.GraphicId).HasColumnType("smallint(6) unsigned").HasColumnName("graphic_id");

                entity.Property(e => e.ReqItemCount1).HasColumnType("int(11) unsigned").HasColumnName("req_item_count_1").HasDefaultValueSql("'1'");

                entity.Property(e => e.ReqItemCount2).HasColumnType("int(11) unsigned").HasColumnName("req_item_count_2").HasDefaultValueSql("'1'");

                entity.Property(e => e.ReqItemId2).HasColumnType("smallint(6) unsigned").HasColumnName("req_item_id_2");

                entity.Property(e => e.ReqSkillCount1).HasColumnType("tinyint(3) unsigned").HasColumnName("req_skill_count_1");

                entity.Property(e => e.ReqSkillCount2).HasColumnType("tinyint(3) unsigned").HasColumnName("req_skill_count_2");

                entity.Property(e => e.ReqSkillCount3).HasColumnType("tinyint(3) unsigned").HasColumnName("req_skill_count_3");

                entity.Property(e => e.ReqSkillId1).HasColumnType("tinyint(3)").HasColumnName("req_skill_id_1").HasDefaultValueSql("'-1'");

                entity.Property(e => e.ReqSkillId2).HasColumnType("tinyint(3)").HasColumnName("req_skill_id_2").HasDefaultValueSql("'-1'");

                entity.Property(e => e.ReqSkillId3).HasColumnType("tinyint(3)").HasColumnName("req_skill_id_3").HasDefaultValueSql("'-1'");

                entity.Property(e => e.RewItemCount).HasColumnType("int(11) unsigned").HasColumnName("rew_item_count").HasDefaultValueSql("'1'");

                entity.Property(e => e.RewItemId).HasColumnType("smallint(6) unsigned").HasColumnName("rew_item_id");

                entity.Property(e => e.RewSkillExp1).HasPrecision(11, 2).HasColumnName("rew_skill_exp_1");

                entity.Property(e => e.RewSkillExp2).HasPrecision(11, 2).HasColumnName("rew_skill_exp_2");

                entity.Property(e => e.RewSkillExp3).HasPrecision(11, 2).HasColumnName("rew_skill_exp_3");

                entity.Property(e => e.RewSkillId1).HasColumnType("tinyint(3)").HasColumnName("rew_skill_id_1").HasDefaultValueSql("'-1'");

                entity.Property(e => e.RewSkillId2).HasColumnType("tinyint(3)").HasColumnName("rew_skill_id_2").HasDefaultValueSql("'-1'");

                entity.Property(e => e.RewSkillId3).HasColumnType("tinyint(3)").HasColumnName("rew_skill_id_3").HasDefaultValueSql("'-1'");
            });

            modelBuilder.Entity<ItemDefinition>(entity =>
            {
                entity.ToTable("item_definitions");

                entity.HasCharSet("latin1").UseCollation("latin1_swedish_ci");

                entity.Property(e => e.Id).HasColumnType("smallint(6) unsigned").ValueGeneratedNever().HasColumnName("id");

                entity.Property(e => e.Examine).HasColumnType("text").HasColumnName("examine");

                entity.Property(e => e.HighAlchemyValue).HasColumnType("int(10)").HasColumnName("high_alchemy_value");

                entity.Property(e => e.LowAlchemyValue).HasColumnType("int(10)").HasColumnName("low_alchemy_value");

                entity.Property(e => e.Name).HasMaxLength(50).HasColumnName("name").HasDefaultValueSql("'unknown'");

                entity.Property(e => e.TradePrice).HasColumnType("int(10)").HasColumnName("trade_price");

                entity.Property(e => e.Tradeable).HasColumnType("tinyint(1) unsigned").HasColumnName("tradeable").HasDefaultValueSql("'1'");

                entity.Property(e => e.Weight).HasPrecision(6, 2).HasColumnName("weight");
            });

            modelBuilder.Entity<ItemLoot>(entity =>
            {
                entity.ToTable("item_loot");

                entity.HasCharSet("latin1").UseCollation("latin1_swedish_ci");

                entity.Property(e => e.Id).HasColumnType("smallint(6) unsigned").HasColumnName("id");

                entity.Property(e => e.MaximumLootCount).HasColumnType("int(11) unsigned").HasColumnName("maximum_loot_count").HasDefaultValueSql("'1'");

                entity.Property(e => e.Name).HasMaxLength(24).HasColumnName("name");

                entity.Property(e => e.RandomizeLootCount).HasColumnType("tinyint(3) unsigned").HasColumnName("randomize_loot_count").HasDefaultValueSql("'1'");

                entity.HasMany(d => d.ItemLootChildren)
                    .WithMany(p => p.ItemLootParents)
                    .UsingEntity<Dictionary<string, object>>("ItemLootrecursion",
                        l => l.HasOne<ItemLoot>().WithMany().HasForeignKey("ItemLootChildId").HasConstraintName("item_loot_table_child_id_foreign_key"),
                        r => r.HasOne<ItemLoot>().WithMany().HasForeignKey("ItemLootParentId").HasConstraintName("item_loot_table_parent_id_foreign_key"),
                        j =>
                        {
                            j.HasKey("ItemLootChildId", "ItemLootParentId")
                                .HasName("PRIMARY")
                                .HasAnnotation("MySql:IndexPrefixLength",
                                    new[]
                                    {
                                        0, 0
                                    });

                            j.ToTable("item_lootrecursion").HasCharSet("utf8").UseCollation("utf8_general_ci");

                            j.HasIndex(new[]
                                {
                                    "ItemLootParentId"
                                },
                                "item_loot_table_parent_id_foreign_key");

                            j.IndexerProperty<ushort>("ItemLootChildId").HasColumnType("smallint(6) unsigned").HasColumnName("item_loot_child_id");

                            j.IndexerProperty<ushort>("ItemLootParentId").HasColumnType("smallint(6) unsigned").HasColumnName("item_loot_parent_id");
                        });

                entity.HasMany(d => d.ItemLootParents)
                    .WithMany(p => p.ItemLootChildren)
                    .UsingEntity<Dictionary<string, object>>("ItemLootrecursion",
                        l => l.HasOne<ItemLoot>().WithMany().HasForeignKey("ItemLootParentId").HasConstraintName("item_loot_table_parent_id_foreign_key"),
                        r => r.HasOne<ItemLoot>().WithMany().HasForeignKey("ItemLootChildId").HasConstraintName("item_loot_table_child_id_foreign_key"),
                        j =>
                        {
                            j.HasKey("ItemLootChildId", "ItemLootParentId")
                                .HasName("PRIMARY")
                                .HasAnnotation("MySql:IndexPrefixLength",
                                    new[]
                                    {
                                        0, 0
                                    });

                            j.ToTable("item_lootrecursion").HasCharSet("utf8").UseCollation("utf8_general_ci");

                            j.HasIndex(new[]
                                {
                                    "ItemLootParentId"
                                },
                                "item_loot_table_parent_id_foreign_key");

                            j.IndexerProperty<ushort>("ItemLootChildId").HasColumnType("smallint(6) unsigned").HasColumnName("item_loot_child_id");

                            j.IndexerProperty<ushort>("ItemLootParentId").HasColumnType("smallint(6) unsigned").HasColumnName("item_loot_parent_id");
                        });
            });

            modelBuilder.Entity<ItemLootItem>(entity =>
            {
                entity.ToTable("item_loot_items");

                entity.HasCharSet("latin1").UseCollation("latin1_swedish_ci");

                entity.HasIndex(e => e.ItemLootId, "item_loot_id_foreign_key");

                entity.Property(e => e.Id).HasColumnType("int(11) unsigned").HasColumnName("id");

                entity.Property(e => e.Always).HasColumnType("tinyint(1) unsigned").HasColumnName("always");

                entity.Property(e => e.ItemId).HasColumnType("smallint(6) unsigned").HasColumnName("item_id");

                entity.Property(e => e.ItemLootId).HasColumnType("smallint(6) unsigned").HasColumnName("item_loot_id");

                entity.Property(e => e.MaximumCount).HasColumnType("int(11) unsigned").HasColumnName("maximum_count").HasDefaultValueSql("'1'");

                entity.Property(e => e.MinimumCount).HasColumnType("int(11) unsigned").HasColumnName("minimum_count");

                entity.Property(e => e.Probability).HasPrecision(8, 3).HasColumnName("probability");

                entity.HasOne(d => d.ItemLoot).WithMany(p => p.ItemLootItems).HasForeignKey(d => d.ItemLootId).HasConstraintName("item_loot_id_foreign_key");
            });

            modelBuilder.Entity<ItemSpawn>(entity =>
            {
                entity.HasKey(e => new
                    {
                        e.ItemId, e.CoordX, e.CoordY, e.CoordZ
                    })
                    .HasName("PRIMARY")
                    .HasAnnotation("MySql:IndexPrefixLength",
                        new[]
                        {
                            0, 0, 0, 0
                        });

                entity.ToTable("item_spawns");

                entity.HasCharSet("latin1").UseCollation("latin1_swedish_ci");

                entity.Property(e => e.ItemId).HasColumnName("item_id");

                entity.Property(e => e.CoordX).HasColumnName("coord_x").HasDefaultValueSql("'3200'");

                entity.Property(e => e.CoordY).HasColumnName("coord_y").HasDefaultValueSql("'3200'");

                entity.Property(e => e.CoordZ).HasColumnName("coord_z");

                entity.Property(e => e.Count).HasColumnName("count").HasDefaultValueSql("'1'");

                entity.Property(e => e.RespawnTicks).HasColumnName("respawn_ticks").HasDefaultValueSql("'100'");
            });

            modelBuilder.Entity<LogsActivity>(entity =>
            {
                entity.ToTable("logs_activities");

                entity.HasCharSet("latin1").UseCollation("latin1_swedish_ci");

                entity.Property(e => e.Id).HasColumnType("bigint(19) unsigned").HasColumnName("id");

                entity.Property(e => e.Date).HasColumnType("datetime").HasColumnName("date");

                entity.Property(e => e.FullDesc).HasColumnType("tinytext").HasColumnName("full_desc");

                entity.Property(e => e.MasterId).HasColumnType("int(11) unsigned").HasColumnName("master_id");

                entity.Property(e => e.ShortDesc).HasColumnType("tinytext").HasColumnName("short_desc");
            });

            modelBuilder.Entity<LogsChat>(entity =>
            {
                entity.ToTable("logs_chat");

                entity.HasCharSet("latin1").UseCollation("latin1_swedish_ci");

                entity.Property(e => e.Id).HasColumnType("bigint(19) unsigned").HasColumnName("id");

                entity.Property(e => e.Date).HasColumnType("datetime").HasColumnName("date");

                entity.Property(e => e.MasterId).HasColumnType("int(11) unsigned").HasColumnName("master_id");

                entity.Property(e => e.Message).HasMaxLength(255).HasColumnName("message");

                entity.Property(e => e.ReceiverId).HasColumnType("int(11) unsigned").HasColumnName("receiver_id");

                entity.Property(e => e.Type).HasColumnType("tinyint(4) unsigned").HasColumnName("type");
            });

            modelBuilder.Entity<LogsConnection>(entity =>
            {
                entity.HasKey(e => new
                    {
                        e.Id, e.Port
                    })
                    .HasName("PRIMARY")
                    .HasAnnotation("MySql:IndexPrefixLength",
                        new[]
                        {
                            0, 0
                        });

                entity.ToTable("logs_connections");

                entity.HasCharSet("latin1").UseCollation("latin1_swedish_ci");

                entity.Property(e => e.Id).ValueGeneratedOnAdd().HasColumnName("id");

                entity.Property(e => e.Port).HasColumnName("port");

                entity.Property(e => e.Ip)
                    .HasMaxLength(40)
                    .HasColumnName("ip")
                    .HasDefaultValueSql("''")
                    .UseCollation("utf8mb4_0900_ai_ci")
                    .HasCharSet("utf8mb4");

                entity.Property(e => e.Time).HasColumnType("datetime").HasColumnName("time");
            });

            modelBuilder.Entity<LogsDisplayNameChange>(entity =>
            {
                entity.ToTable("logs_display_name_changes");

                entity.HasCharSet("latin1").UseCollation("latin1_swedish_ci");

                entity.Property(e => e.Id).HasColumnType("bigint(19) unsigned").HasColumnName("id");

                entity.Property(e => e.DateChanged).HasColumnType("datetime").HasColumnName("date_changed");

                entity.Property(e => e.MasterId).HasColumnType("int(11) unsigned").HasColumnName("master_id");

                entity.Property(e => e.NewDisplayName).HasMaxLength(12).HasColumnName("new_display_name");

                entity.Property(e => e.PreviousDisplayName).HasMaxLength(12).HasColumnName("previous_display_name");
            });

            modelBuilder.Entity<LogsLoginAttempt>(entity =>
            {
                entity.ToTable("logs_login_attempts");

                entity.HasCharSet("latin1").UseCollation("latin1_swedish_ci");

                entity.Property(e => e.Id).HasColumnType("bigint(19) unsigned").HasColumnName("id");

                entity.Property(e => e.Attempt).HasColumnType("tinyint(4)").HasColumnName("attempt");

                entity.Property(e => e.Date).HasColumnType("datetime").HasColumnName("date");

                entity.Property(e => e.Ip).HasMaxLength(40).HasColumnName("ip");

                entity.Property(e => e.MasterId).HasColumnType("int(11) unsigned").HasColumnName("master_id");

                entity.Property(e => e.Type).HasColumnType("tinyint(4)").HasColumnName("type");
            });

            modelBuilder.Entity<MinigamesBarrow>(entity =>
            {
                entity.HasKey(e => e.MasterId).HasName("PRIMARY");

                entity.ToTable("minigames_barrows");

                entity.HasCharSet("utf8").UseCollation("utf8_general_ci");

                entity.Property(e => e.MasterId).HasColumnType("int(11) unsigned").ValueGeneratedNever().HasColumnName("master_id");

                entity.Property(e => e.BrotherKilled0).HasColumnType("tinyint(1) unsigned").HasColumnName("brother_killed_0");

                entity.Property(e => e.BrotherKilled1).HasColumnType("tinyint(1) unsigned").HasColumnName("brother_killed_1");

                entity.Property(e => e.BrotherKilled2).HasColumnType("tinyint(1) unsigned").HasColumnName("brother_killed_2");

                entity.Property(e => e.BrotherKilled3).HasColumnType("tinyint(1) unsigned").HasColumnName("brother_killed_3");

                entity.Property(e => e.BrotherKilled4).HasColumnType("tinyint(1) unsigned").HasColumnName("brother_killed_4");

                entity.Property(e => e.BrotherKilled5).HasColumnType("tinyint(1) unsigned").HasColumnName("brother_killed_5");

                entity.Property(e => e.BrotherKilled6).HasColumnType("tinyint(1) unsigned").HasColumnName("brother_killed_6");

                entity.Property(e => e.CryptStartIndex).HasColumnType("tinyint(1) unsigned").HasColumnName("crypt_start_index");

                entity.Property(e => e.KillCount).HasColumnType("int(11)").HasColumnName("kill_count");

                entity.Property(e => e.LootedChest).HasColumnType("tinyint(1) unsigned").HasColumnName("looted_chest");

                entity.Property(e => e.TunnelIndex).HasColumnType("tinyint(1) unsigned").HasColumnName("tunnel_index");

                entity.HasOne(d => d.Master)
                    .WithOne(p => p.MinigamesBarrow)
                    .HasForeignKey<MinigamesBarrow>(d => d.MasterId)
                    .HasConstraintName("master_id_foreign_key_20");
            });

            modelBuilder.Entity<MinigamesDuelArena>(entity =>
            {
                entity.HasKey(e => e.MasterId).HasName("PRIMARY");

                entity.ToTable("minigames_duel_arena");

                entity.HasCharSet("utf8").UseCollation("utf8_general_ci");

                entity.Property(e => e.MasterId).HasColumnType("int(11) unsigned").ValueGeneratedNever().HasColumnName("master_id");

                entity.Property(e => e.FavouriteRules).HasColumnType("text").HasColumnName("favourite_rules");

                entity.Property(e => e.PreviousRules).HasColumnType("text").HasColumnName("previous_rules");

                entity.HasOne(d => d.Master)
                    .WithOne(p => p.MinigamesDuelArena)
                    .HasForeignKey<MinigamesDuelArena>(d => d.MasterId)
                    .HasConstraintName("master_id_foreign_key_21");
            });

            modelBuilder.Entity<MinigamesGodwar>(entity =>
            {
                entity.HasKey(e => e.MasterId).HasName("PRIMARY");

                entity.ToTable("minigames_godwars");

                entity.HasCharSet("utf8").UseCollation("utf8_general_ci");

                entity.Property(e => e.MasterId).HasColumnType("int(11) unsigned").ValueGeneratedNever().HasColumnName("master_id");

                entity.Property(e => e.ArmadylKillCount).HasColumnType("smallint(6)").HasColumnName("armadyl_kill_count");

                entity.Property(e => e.BandosKillCount).HasColumnType("smallint(6)").HasColumnName("bandos_kill_count");

                entity.Property(e => e.SaradominKillCount).HasColumnType("smallint(6)").HasColumnName("saradomin_kill_count");

                entity.Property(e => e.ZamorakKillCount).HasColumnType("smallint(6)").HasColumnName("zamorak_kill_count");

                entity.HasOne(d => d.Master)
                    .WithOne(p => p.MinigamesGodwar)
                    .HasForeignKey<MinigamesGodwar>(d => d.MasterId)
                    .HasConstraintName("master_id_foreign_key_22");
            });

            modelBuilder.Entity<MinigamesTzhaarCave>(entity =>
            {
                entity.HasKey(e => e.MasterId).HasName("PRIMARY");

                entity.ToTable("minigames_tzhaar_cave");

                entity.HasCharSet("utf8").UseCollation("utf8_general_ci");

                entity.HasIndex(e => e.CurrentWaveId, "current_wave_id_foreign_key");

                entity.Property(e => e.MasterId).HasColumnType("int(11) unsigned").ValueGeneratedNever().HasColumnName("master_id");

                entity.Property(e => e.CurrentWaveId).HasColumnType("int(11) unsigned").HasColumnName("current_wave_id");

                entity.HasOne(d => d.Master)
                    .WithOne(p => p.MinigamesTzhaarCave)
                    .HasForeignKey<MinigamesTzhaarCave>(d => d.MasterId)
                    .HasConstraintName("master_id_foreign_key_23");
            });

            modelBuilder.Entity<MinigamesTzhaarCaveWave>(entity =>
            {
                entity.HasKey(e => new
                    {
                        e.WaveId, e.NpcId
                    })
                    .HasName("PRIMARY")
                    .HasAnnotation("MySql:IndexPrefixLength",
                        new[]
                        {
                            0, 0
                        });

                entity.ToTable("minigames_tzhaar_cave_waves");

                entity.HasCharSet("utf8").UseCollation("utf8_general_ci");

                entity.HasIndex(e => e.WaveId, "wave_id");

                entity.Property(e => e.WaveId).HasColumnType("int(11) unsigned").HasColumnName("wave_id");

                entity.Property(e => e.NpcId).HasColumnType("smallint(6) unsigned").HasColumnName("npc_id");

                entity.Property(e => e.Count).HasColumnType("int(11)").HasColumnName("count").HasDefaultValueSql("'1'");
            });

            modelBuilder.Entity<MusicDefinition>(entity =>
            {
                entity.ToTable("music_definitions");

                entity.HasCharSet("utf8").UseCollation("utf8_general_ci");

                entity.Property(e => e.Id).HasColumnType("smallint(6) unsigned").ValueGeneratedNever().HasColumnName("id");

                entity.Property(e => e.Hint).HasMaxLength(255).HasColumnName("hint");

                entity.Property(e => e.Name).HasMaxLength(32).HasColumnName("name");
            });

            modelBuilder.Entity<MusicLocation>(entity =>
            {
                entity.HasKey(e => new
                    {
                        e.MusicId, e.RegionId
                    })
                    .HasName("PRIMARY")
                    .HasAnnotation("MySql:IndexPrefixLength",
                        new[]
                        {
                            0, 0
                        });

                entity.ToTable("music_locations");

                entity.HasCharSet("utf8").UseCollation("utf8_general_ci");

                entity.Property(e => e.MusicId).HasColumnType("smallint(6) unsigned").HasColumnName("music_id");

                entity.Property(e => e.RegionId).HasColumnType("int(11) unsigned").HasColumnName("region_id");

                entity.HasOne(d => d.Music).WithMany(p => p.MusicLocations).HasForeignKey(d => d.MusicId).HasConstraintName("music_id_foreign_key");
            });

            modelBuilder.Entity<NpcBonuses>(entity =>
            {
                entity.HasKey(e => e.NpcId).HasName("PRIMARY");

                entity.ToTable("npc_bonuses");

                entity.HasCharSet("latin1").UseCollation("latin1_swedish_ci");

                entity.Property(e => e.NpcId).HasColumnType("smallint(6) unsigned").ValueGeneratedNever().HasColumnName("npc_id");

                entity.Property(e => e.AbsorbMagic).HasColumnType("smallint(8)").HasColumnName("absorb_magic");

                entity.Property(e => e.AbsorbMelee).HasColumnType("smallint(8)").HasColumnName("absorb_melee");

                entity.Property(e => e.AbsorbRange).HasColumnType("smallint(8)").HasColumnName("absorb_range");

                entity.Property(e => e.AttackCrush).HasColumnType("smallint(8)").HasColumnName("attack_crush");

                entity.Property(e => e.AttackMagic).HasColumnType("smallint(8)").HasColumnName("attack_magic");

                entity.Property(e => e.AttackRanged).HasColumnType("smallint(8)").HasColumnName("attack_ranged");

                entity.Property(e => e.AttackSlash).HasColumnType("smallint(8)").HasColumnName("attack_slash");

                entity.Property(e => e.AttackStab).HasColumnType("smallint(8)").HasColumnName("attack_stab");

                entity.Property(e => e.DefenceCrush).HasColumnType("smallint(8)").HasColumnName("defence_crush");

                entity.Property(e => e.DefenceMagic).HasColumnType("smallint(8)").HasColumnName("defence_magic");

                entity.Property(e => e.DefenceRanged).HasColumnType("smallint(8)").HasColumnName("defence_ranged");

                entity.Property(e => e.DefenceSlash).HasColumnType("smallint(8)").HasColumnName("defence_slash");

                entity.Property(e => e.DefenceStab).HasColumnType("smallint(8)").HasColumnName("defence_stab");

                entity.Property(e => e.DefenceSummoning).HasColumnType("smallint(8)").HasColumnName("defence_summoning");

                entity.Property(e => e.Magic).HasColumnType("smallint(8)").HasColumnName("magic");

                entity.Property(e => e.Prayer).HasColumnType("smallint(8)").HasColumnName("prayer");

                entity.Property(e => e.RangedStrength).HasColumnType("smallint(8)").HasColumnName("ranged_strength");

                entity.Property(e => e.Strength).HasColumnType("smallint(8)").HasColumnName("strength");
            });

            modelBuilder.Entity<NpcDefinition>(entity =>
            {
                entity.HasKey(e => e.NpcId).HasName("PRIMARY");

                entity.ToTable("npc_definitions");

                entity.HasCharSet("latin1").UseCollation("latin1_swedish_ci");

                entity.HasIndex(e => e.NpcLootId, "npc_loot_id_foreign_key");

                entity.HasIndex(e => e.NpcPickpocketingLootId, "npc_pickpocket_loot_id_foreign_key");

                entity.Property(e => e.NpcId).ValueGeneratedNever().HasColumnName("npc_id");

                entity.Property(e => e.AttackAnimation).HasColumnName("attack_animation").HasDefaultValueSql("'422'");

                entity.Property(e => e.AttackGraphic).HasColumnName("attack_graphic").HasDefaultValueSql("'-1'");

                entity.Property(e => e.AttackSpeed).HasColumnName("attack_speed").HasDefaultValueSql("'8'");

                entity.Property(e => e.Attackable).HasColumnName("attackable").HasDefaultValueSql("'1'");

                entity.Property(e => e.BoundsType).HasColumnName("bounds_type").HasDefaultValueSql("'1'");

                entity.Property(e => e.CombatLevel).HasColumnName("combat_level").HasDefaultValueSql("'1'");

                entity.Property(e => e.DeathAnimation).HasColumnName("death_animation").HasDefaultValueSql("'7197'");

                entity.Property(e => e.DeathGraphic).HasColumnName("death_graphic").HasDefaultValueSql("'-1'");

                entity.Property(e => e.DeathTicks).HasColumnName("death_ticks").HasDefaultValueSql("'7'");

                entity.Property(e => e.DefenceAnimation).HasColumnName("defence_animation").HasDefaultValueSql("'404'");

                entity.Property(e => e.DefenceGraphic).HasColumnName("defence_graphic").HasDefaultValueSql("'-1'");

                entity.Property(e => e.Examine).HasMaxLength(100).HasColumnName("examine").HasDefaultValueSql("'It''s an npc.'");

                entity.Property(e => e.Name).HasMaxLength(32).HasColumnName("name").HasDefaultValueSql("'unknown'");

                entity.Property(e => e.NpcLootId).HasColumnName("npc_loot_id").HasDefaultValueSql("'0'");

                entity.Property(e => e.NpcPickpocketingLootId).HasColumnName("npc_pickpocketing_loot_id").HasDefaultValueSql("'0'");

                entity.Property(e => e.ReactionType).HasColumnName("reaction_type");

                entity.Property(e => e.RespawnTime).HasColumnName("respawn_time").HasDefaultValueSql("'100'");

                entity.Property(e => e.SlayerLevelRequired).HasColumnName("slayer_level_required");

                entity.Property(e => e.WalksRandomly).HasColumnName("walks_randomly").HasDefaultValueSql("'1'");

                entity.HasOne(d => d.NpcLoot)
                    .WithMany(p => p.NpcDefinitionNpcLoots)
                    .HasForeignKey(d => d.NpcLootId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("npc_loot_id_foreign_key");

                entity.HasOne(d => d.NpcPickpocketingLoot)
                    .WithMany(p => p.NpcDefinitionNpcPickpocketingLoots)
                    .HasForeignKey(d => d.NpcPickpocketingLootId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("npc_pickpocket_loot_id_foreign_key");
            });

            modelBuilder.Entity<NpcLoot>(entity =>
            {
                entity.ToTable("npc_loot");

                entity.HasCharSet("latin1").UseCollation("latin1_swedish_ci");

                entity.Property(e => e.Id).HasColumnType("smallint(6) unsigned").HasColumnName("id");

                entity.Property(e => e.Always).HasColumnType("tinyint(1) unsigned").HasColumnName("always");

                entity.Property(e => e.MaximumLootCount).HasColumnType("int(11)").HasColumnName("maximum_loot_count").HasDefaultValueSql("'1'");

                entity.Property(e => e.Name).HasMaxLength(32).HasColumnName("name").HasDefaultValueSql("''");

                entity.Property(e => e.RandomizeLootCount).HasColumnType("tinyint(1) unsigned").HasColumnName("randomize_loot_count").HasDefaultValueSql("'1'");

                entity.HasMany(d => d.NpcLootChildren)
                    .WithMany(p => p.NpcLootParents)
                    .UsingEntity<Dictionary<string, object>>("NpcLootrecursion",
                        l => l.HasOne<NpcLoot>().WithMany().HasForeignKey("NpcLootChildId").HasConstraintName("npc_loot_child_foreign_key"),
                        r => r.HasOne<NpcLoot>().WithMany().HasForeignKey("NpcLootParentId").HasConstraintName("npc_loot_parent_foreign_key"),
                        j =>
                        {
                            j.HasKey("NpcLootChildId", "NpcLootParentId")
                                .HasName("PRIMARY")
                                .HasAnnotation("MySql:IndexPrefixLength",
                                    new[]
                                    {
                                        0, 0
                                    });

                            j.ToTable("npc_lootrecursion").HasCharSet("latin1").UseCollation("latin1_swedish_ci");

                            j.HasIndex(new[]
                                {
                                    "NpcLootParentId"
                                },
                                "npc_loot_parent_foreign_key");

                            j.IndexerProperty<ushort>("NpcLootChildId").HasColumnType("smallint(6) unsigned").HasColumnName("npc_loot_child_id");

                            j.IndexerProperty<ushort>("NpcLootParentId").HasColumnType("smallint(6) unsigned").HasColumnName("npc_loot_parent_id");
                        });

                entity.HasMany(d => d.NpcLootParents)
                    .WithMany(p => p.NpcLootChildren)
                    .UsingEntity<Dictionary<string, object>>("NpcLootrecursion",
                        l => l.HasOne<NpcLoot>().WithMany().HasForeignKey("NpcLootParentId").HasConstraintName("npc_loot_parent_foreign_key"),
                        r => r.HasOne<NpcLoot>().WithMany().HasForeignKey("NpcLootChildId").HasConstraintName("npc_loot_child_foreign_key"),
                        j =>
                        {
                            j.HasKey("NpcLootChildId", "NpcLootParentId")
                                .HasName("PRIMARY")
                                .HasAnnotation("MySql:IndexPrefixLength",
                                    new[]
                                    {
                                        0, 0
                                    });

                            j.ToTable("npc_lootrecursion").HasCharSet("latin1").UseCollation("latin1_swedish_ci");

                            j.HasIndex(new[]
                                {
                                    "NpcLootParentId"
                                },
                                "npc_loot_parent_foreign_key");

                            j.IndexerProperty<ushort>("NpcLootChildId").HasColumnType("smallint(6) unsigned").HasColumnName("npc_loot_child_id");

                            j.IndexerProperty<ushort>("NpcLootParentId").HasColumnType("smallint(6) unsigned").HasColumnName("npc_loot_parent_id");
                        });
            });

            modelBuilder.Entity<NpcLootItem>(entity =>
            {
                entity.ToTable("npc_loot_items");

                entity.HasCharSet("latin1").UseCollation("latin1_swedish_ci");

                entity.HasIndex(e => e.NpcLootId, "npc_loot_id_foreign_key_1");

                entity.Property(e => e.Id).HasColumnType("int(11) unsigned").HasColumnName("id");

                entity.Property(e => e.Always).HasColumnType("tinyint(1) unsigned").HasColumnName("always");

                entity.Property(e => e.ItemId).HasColumnType("smallint(6) unsigned").HasColumnName("item_id");

                entity.Property(e => e.MaximumCount).HasColumnType("int(11) unsigned").HasColumnName("maximum_count").HasDefaultValueSql("'1'");

                entity.Property(e => e.MinimumCount).HasColumnType("int(11) unsigned").HasColumnName("minimum_count");

                entity.Property(e => e.NpcLootId).HasColumnType("smallint(6) unsigned").HasColumnName("npc_loot_id");

                entity.Property(e => e.Probability).HasPrecision(8, 3).HasColumnName("probability");

                entity.HasOne(d => d.NpcLoot).WithMany(p => p.NpcLootItems).HasForeignKey(d => d.NpcLootId).HasConstraintName("npc_loot_id_foreign_key_1");
            });

            modelBuilder.Entity<NpcSpawn>(entity =>
            {
                entity.HasKey(e => e.SpawnId).HasName("PRIMARY");

                entity.ToTable("npc_spawns");

                entity.HasCharSet("latin1").UseCollation("latin1_swedish_ci");

                entity.Property(e => e.SpawnId).HasColumnName("spawn_id");

                entity.Property(e => e.CoordX).HasColumnName("coord_x").HasDefaultValueSql("'3222'");

                entity.Property(e => e.CoordY).HasColumnName("coord_y").HasDefaultValueSql("'3222'");

                entity.Property(e => e.CoordZ).HasColumnName("coord_z");

                entity.Property(e => e.MaxCoordX).HasColumnName("max_coord_x");

                entity.Property(e => e.MaxCoordY).HasColumnName("max_coord_y");

                entity.Property(e => e.MaxCoordZ).HasColumnName("max_coord_z");

                entity.Property(e => e.MinCoordX).HasColumnName("min_coord_x").HasDefaultValueSql("'3200'");

                entity.Property(e => e.MinCoordY).HasColumnName("min_coord_y").HasDefaultValueSql("'3200'");

                entity.Property(e => e.MinCoordZ).HasColumnName("min_coord_z");

                entity.Property(e => e.NpcId).HasColumnName("npc_id");

                entity.Property(e => e.SpawnDirection).HasColumnName("spawn_direction");
            });

            modelBuilder.Entity<NpcStatistic>(entity =>
            {
                entity.HasKey(e => e.NpcId).HasName("PRIMARY");

                entity.ToTable("npc_statistics");

                entity.HasCharSet("latin1").UseCollation("latin1_swedish_ci");

                entity.Property(e => e.NpcId).HasColumnType("smallint(6) unsigned").ValueGeneratedNever().HasColumnName("npc_id");

                entity.Property(e => e.AttackLevel).HasColumnType("smallint(8) unsigned").HasColumnName("attack_level").HasDefaultValueSql("'1'");

                entity.Property(e => e.DefenceLevel).HasColumnType("smallint(8) unsigned").HasColumnName("defence_level").HasDefaultValueSql("'1'");

                entity.Property(e => e.MagicLevel).HasColumnType("smallint(8) unsigned").HasColumnName("magic_level").HasDefaultValueSql("'1'");

                entity.Property(e => e.MaxLifepoints).HasColumnType("smallint(8) unsigned").HasColumnName("max_lifepoints").HasDefaultValueSql("'10'");

                entity.Property(e => e.RangedLevel).HasColumnType("smallint(8) unsigned").HasColumnName("ranged_level").HasDefaultValueSql("'1'");

                entity.Property(e => e.StrengthLevel).HasColumnType("smallint(8) unsigned").HasColumnName("strength_level").HasDefaultValueSql("'1'");
            });

            modelBuilder.Entity<ProfanityWord>(entity =>
            {
                entity.HasKey(e => e.Word).HasName("PRIMARY");

                entity.ToTable("profanity_words");

                entity.HasCharSet("utf8").UseCollation("utf8_general_ci");

                entity.Property(e => e.Word).HasColumnName("word");
            });

            modelBuilder.Entity<Quest>(entity =>
            {
                entity.ToTable("quests");

                entity.HasCharSet("latin1").UseCollation("latin1_swedish_ci");

                entity.Property(e => e.Id).HasColumnType("smallint(6) unsigned").ValueGeneratedNever().HasColumnName("id");

                entity.Property(e => e.MinSkillCount1).HasColumnType("tinyint(3) unsigned").HasColumnName("min_skill_count_1");

                entity.Property(e => e.MinSkillCount2).HasColumnType("tinyint(3) unsigned").HasColumnName("min_skill_count_2");

                entity.Property(e => e.MinSkillCount3).HasColumnType("tinyint(3) unsigned").HasColumnName("min_skill_count_3");

                entity.Property(e => e.MinSkillCount4).HasColumnType("tinyint(3) unsigned").HasColumnName("min_skill_count_4");

                entity.Property(e => e.MinSkillId1).HasColumnType("tinyint(3) unsigned").HasColumnName("min_skill_id_1");

                entity.Property(e => e.MinSkillId2).HasColumnType("tinyint(3) unsigned").HasColumnName("min_skill_id_2");

                entity.Property(e => e.MinSkillId3).HasColumnType("tinyint(3) unsigned").HasColumnName("min_skill_id_3");

                entity.Property(e => e.MinSkillId4).HasColumnType("tinyint(3) unsigned").HasColumnName("min_skill_id_4");

                entity.Property(e => e.Name).HasMaxLength(40).HasColumnName("name").HasDefaultValueSql("''");

                entity.Property(e => e.ReqItemCount1).HasColumnType("int(11) unsigned").HasColumnName("req_item_count_1");

                entity.Property(e => e.ReqItemCount2).HasColumnType("int(11) unsigned").HasColumnName("req_item_count_2");

                entity.Property(e => e.ReqItemCount3).HasColumnType("int(11) unsigned").HasColumnName("req_item_count_3");

                entity.Property(e => e.ReqItemCount4).HasColumnType("int(11) unsigned").HasColumnName("req_item_count_4");

                entity.Property(e => e.ReqItemId1).HasColumnType("smallint(6) unsigned").HasColumnName("req_item_id_1");

                entity.Property(e => e.ReqItemId2).HasColumnType("smallint(6) unsigned").HasColumnName("req_item_id_2");

                entity.Property(e => e.ReqItemId3).HasColumnType("smallint(6) unsigned").HasColumnName("req_item_id_3");

                entity.Property(e => e.ReqItemId4).HasColumnType("smallint(6) unsigned").HasColumnName("req_item_id_4");

                entity.Property(e => e.ReqNpcCount1).HasColumnType("int(11) unsigned").HasColumnName("req_npc_count_1");

                entity.Property(e => e.ReqNpcCount2).HasColumnType("int(11) unsigned").HasColumnName("req_npc_count_2");

                entity.Property(e => e.ReqNpcCount3).HasColumnType("int(11) unsigned").HasColumnName("req_npc_count_3");

                entity.Property(e => e.ReqNpcCount4).HasColumnType("int(11) unsigned").HasColumnName("req_npc_count_4");

                entity.Property(e => e.ReqNpcId1).HasColumnType("smallint(6) unsigned").HasColumnName("req_npc_id_1");

                entity.Property(e => e.ReqNpcId2).HasColumnType("smallint(6) unsigned").HasColumnName("req_npc_id_2");

                entity.Property(e => e.ReqNpcId3).HasColumnType("smallint(6) unsigned").HasColumnName("req_npc_id_3");

                entity.Property(e => e.ReqNpcId4).HasColumnType("smallint(6) unsigned").HasColumnName("req_npc_id_4");

                entity.Property(e => e.ReqQuestId1).HasColumnType("smallint(6) unsigned").HasColumnName("req_quest_id_1");

                entity.Property(e => e.ReqQuestId2).HasColumnType("smallint(6) unsigned").HasColumnName("req_quest_id_2");

                entity.Property(e => e.ReqQuestId3).HasColumnType("smallint(6) unsigned").HasColumnName("req_quest_id_3");

                entity.Property(e => e.ReqQuestId4).HasColumnType("smallint(6) unsigned").HasColumnName("req_quest_id_4");

                entity.Property(e => e.RewItemCount1).HasColumnType("int(11) unsigned").HasColumnName("rew_item_count_1");

                entity.Property(e => e.RewItemCount2).HasColumnType("int(11) unsigned").HasColumnName("rew_item_count_2");

                entity.Property(e => e.RewItemCount3).HasColumnType("int(11) unsigned").HasColumnName("rew_item_count_3");

                entity.Property(e => e.RewItemCount4).HasColumnType("int(11) unsigned").HasColumnName("rew_item_count_4");

                entity.Property(e => e.RewItemId1).HasColumnType("smallint(6) unsigned").HasColumnName("rew_item_id_1");

                entity.Property(e => e.RewItemId2).HasColumnType("smallint(6) unsigned").HasColumnName("rew_item_id_2");

                entity.Property(e => e.RewItemId3).HasColumnType("smallint(6) unsigned").HasColumnName("rew_item_id_3");

                entity.Property(e => e.RewItemId4).HasColumnType("smallint(6) unsigned").HasColumnName("rew_item_id_4");

                entity.Property(e => e.RewQuestPoints).HasColumnType("smallint(6) unsigned").HasColumnName("rew_quest_points");

                entity.Property(e => e.RewSkillExp1).HasColumnType("int(11) unsigned").HasColumnName("rew_skill_exp_1");

                entity.Property(e => e.RewSkillExp2).HasColumnType("int(11) unsigned").HasColumnName("rew_skill_exp_2");

                entity.Property(e => e.RewSkillExp3).HasColumnType("int(11) unsigned").HasColumnName("rew_skill_exp_3");

                entity.Property(e => e.RewSkillExp4).HasColumnType("int(11) unsigned").HasColumnName("rew_skill_exp_4");

                entity.Property(e => e.RewSkillId1).HasColumnType("tinyint(3) unsigned").HasColumnName("rew_skill_id_1");

                entity.Property(e => e.RewSkillId2).HasColumnType("tinyint(3) unsigned").HasColumnName("rew_skill_id_2");

                entity.Property(e => e.RewSkillId3).HasColumnType("tinyint(3) unsigned").HasColumnName("rew_skill_id_3");

                entity.Property(e => e.RewSkillId4).HasColumnType("tinyint(3) unsigned").HasColumnName("rew_skill_id_4");
            });

            modelBuilder.Entity<ReservedName>(entity =>
            {
                entity.HasKey(e => e.Name).HasName("PRIMARY");

                entity.ToTable("reserved_names");

                entity.HasCharSet("utf8").UseCollation("utf8_general_ci");

                entity.Property(e => e.Name).HasMaxLength(64).HasColumnName("name").HasDefaultValueSql("''");
            });

            modelBuilder.Entity<Shop>(entity =>
            {
                entity.ToTable("shops");

                entity.HasCharSet("latin1").UseCollation("latin1_swedish_ci");

                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.Capacity).HasColumnName("capacity").HasDefaultValueSql("'1'");

                entity.Property(e => e.CurrencyId).HasColumnName("currency_id").HasDefaultValueSql("'995'");

                entity.Property(e => e.GeneralStore).HasColumnName("general_store");

                entity.Property(e => e.MainStockItems).HasColumnType("text").HasColumnName("main_stock_items");

                entity.Property(e => e.Name).HasMaxLength(48).HasColumnName("name").HasDefaultValueSql("''");

                entity.Property(e => e.SampleStockItems).HasColumnName("sample_stock_items").UseCollation("utf8mb4_0900_ai_ci").HasCharSet("utf8mb4");
            });

            modelBuilder.Entity<SkillsCookingFoodDefinition>(entity =>
            {
                entity.HasKey(e => e.ItemId).HasName("PRIMARY");

                entity.ToTable("skills_cooking_food_definitions");

                entity.HasCharSet("utf8").UseCollation("utf8_general_ci");

                entity.Property(e => e.ItemId).HasColumnType("smallint(6) unsigned").ValueGeneratedNever().HasColumnName("item_id");

                entity.Property(e => e.EatingTime).HasColumnType("int(11) unsigned").HasColumnName("eating_time").HasDefaultValueSql("'3'");

                entity.Property(e => e.HealAmount).HasColumnType("tinyint(3) unsigned").HasColumnName("heal_amount");

                entity.Property(e => e.LeftItemId).HasColumnType("smallint(6)").HasColumnName("left_item_id").HasDefaultValueSql("'-1'");
            });

            modelBuilder.Entity<SkillsCookingRawFoodDefinition>(entity =>
            {
                entity.HasKey(e => e.ItemId).HasName("PRIMARY");

                entity.ToTable("skills_cooking_raw_food_definitions");

                entity.HasCharSet("utf8").UseCollation("utf8_general_ci");

                entity.Property(e => e.ItemId).HasColumnType("smallint(6) unsigned").ValueGeneratedNever().HasColumnName("item_id");

                entity.Property(e => e.BurntItemId).HasColumnType("smallint(6) unsigned").HasColumnName("burnt_item_id");

                entity.Property(e => e.CookedItemId).HasColumnType("smallint(6) unsigned").HasColumnName("cooked_item_id");

                entity.Property(e => e.Experience).HasPrecision(11, 2).HasColumnName("experience");

                entity.Property(e => e.RequiredLevel).HasColumnType("tinyint(3) unsigned").HasColumnName("required_level").HasDefaultValueSql("'1'");

                entity.Property(e => e.StopBurningLevel).HasColumnType("tinyint(3) unsigned").HasColumnName("stop_burning_level");
            });

            modelBuilder.Entity<SkillsCraftingGemDefinition>(entity =>
            {
                entity.HasKey(e => e.ResourceId).HasName("PRIMARY");

                entity.ToTable("skills_crafting_gem_definitions");

                entity.HasCharSet("utf8").UseCollation("utf8_general_ci");

                entity.Property(e => e.ResourceId).HasColumnType("smallint(6) unsigned").ValueGeneratedNever().HasColumnName("resource_id");

                entity.Property(e => e.AnimationId).HasColumnType("smallint(6)").HasColumnName("animation_id").HasDefaultValueSql("'-1'");

                entity.Property(e => e.Experience).HasPrecision(11, 2).HasColumnName("experience");

                entity.Property(e => e.ProductId).HasColumnType("smallint(6) unsigned").HasColumnName("product_id");

                entity.Property(e => e.RequiredLevel).HasColumnType("tinyint(3) unsigned").HasColumnName("required_level").HasDefaultValueSql("'1'");
            });

            modelBuilder.Entity<SkillsCraftingJewelryDefinition>(entity =>
            {
                entity.HasKey(e => e.ChildId).HasName("PRIMARY");

                entity.ToTable("skills_crafting_jewelry_definitions");

                entity.HasCharSet("utf8").UseCollation("utf8_general_ci");

                entity.Property(e => e.ChildId).HasColumnType("smallint(6) unsigned").ValueGeneratedNever().HasColumnName("child_id");

                entity.Property(e => e.Experience).HasPrecision(11, 2).HasColumnName("experience");

                entity.Property(e => e.ProductId).HasColumnType("smallint(6) unsigned").HasColumnName("product_id");

                entity.Property(e => e.RequiredLevel).HasColumnType("tinyint(3) unsigned").HasColumnName("required_level").HasDefaultValueSql("'1'");

                entity.Property(e => e.ResourceId).HasColumnType("smallint(6) unsigned").HasColumnName("resource_id");

                entity.Property(e => e.Type).HasColumnType("enum('Amulet','Necklace','Bracelet','Ring')").HasColumnName("type");
            });

            modelBuilder.Entity<SkillsCraftingLeatherDefinition>(entity =>
            {
                entity.HasKey(e => e.ProductId).HasName("PRIMARY");

                entity.ToTable("skills_crafting_leather_definitions");

                entity.HasCharSet("utf8").UseCollation("utf8_general_ci");

                entity.Property(e => e.ProductId).HasColumnType("smallint(6) unsigned").ValueGeneratedNever().HasColumnName("product_id");

                entity.Property(e => e.Experience).HasPrecision(11, 2).HasColumnName("experience");

                entity.Property(e => e.RequiredLevel).HasColumnType("tinyint(3) unsigned").HasColumnName("required_level").HasDefaultValueSql("'1'");

                entity.Property(e => e.RequiredResourceCount)
                    .HasColumnType("int(11) unsigned")
                    .HasColumnName("required_resource_count")
                    .HasDefaultValueSql("'1'");

                entity.Property(e => e.ResourceId).HasColumnType("smallint(6) unsigned").HasColumnName("resource_id");
            });

            modelBuilder.Entity<SkillsCraftingPotteryDefinition>(entity =>
            {
                entity.HasKey(e => e.FormedProductId).HasName("PRIMARY");

                entity.ToTable("skills_crafting_pottery_definitions");

                entity.HasCharSet("utf8").UseCollation("utf8_general_ci");

                entity.Property(e => e.FormedProductId).HasColumnType("smallint(6) unsigned").ValueGeneratedNever().HasColumnName("formed_product_id");

                entity.Property(e => e.BakeExperience).HasPrecision(11, 2).HasColumnName("bake_experience");

                entity.Property(e => e.BakedProductId).HasColumnType("smallint(6) unsigned").HasColumnName("baked_product_id");

                entity.Property(e => e.FormExperience).HasPrecision(11, 2).HasColumnName("form_experience");

                entity.Property(e => e.RequiredLevel).HasColumnType("tinyint(3) unsigned").HasColumnName("required_level");
            });

            modelBuilder.Entity<SkillsCraftingSilverDefinition>(entity =>
            {
                entity.HasKey(e => e.ChildId).HasName("PRIMARY");

                entity.ToTable("skills_crafting_silver_definitions");

                entity.HasCharSet("utf8").UseCollation("utf8_general_ci");

                entity.Property(e => e.ChildId).HasColumnType("smallint(6) unsigned").ValueGeneratedNever().HasColumnName("child_id");

                entity.Property(e => e.Experience).HasPrecision(11, 2).HasColumnName("experience");

                entity.Property(e => e.MouldId).HasColumnType("smallint(6) unsigned").HasColumnName("mould_id");

                entity.Property(e => e.ProductId).HasColumnType("smallint(6) unsigned").HasColumnName("product_id");

                entity.Property(e => e.RequiredLevel).HasColumnType("tinyint(3) unsigned").HasColumnName("required_level").HasDefaultValueSql("'1'");
            });

            modelBuilder.Entity<SkillsCraftingSpinDefinition>(entity =>
            {
                entity.HasKey(e => e.ResourceId).HasName("PRIMARY");

                entity.ToTable("skills_crafting_spin_definitions");

                entity.HasCharSet("utf8").UseCollation("utf8_general_ci");

                entity.Property(e => e.ResourceId).HasColumnType("smallint(6) unsigned").ValueGeneratedNever().HasColumnName("resource_id");

                entity.Property(e => e.Experience).HasPrecision(11, 2).HasColumnName("experience");

                entity.Property(e => e.ProductId).HasColumnType("smallint(6) unsigned").HasColumnName("product_id");

                entity.Property(e => e.RequiredLevel).HasColumnType("tinyint(3) unsigned").HasColumnName("required_level").HasDefaultValueSql("'1'");
            });

            modelBuilder.Entity<SkillsCraftingTanDefinition>(entity =>
            {
                entity.HasKey(e => new
                    {
                        e.ResourceId, e.ProductId
                    })
                    .HasName("PRIMARY")
                    .HasAnnotation("MySql:IndexPrefixLength",
                        new[]
                        {
                            0, 0
                        });

                entity.ToTable("skills_crafting_tan_definitions");

                entity.HasCharSet("utf8").UseCollation("utf8_general_ci");

                entity.Property(e => e.ResourceId).HasColumnType("smallint(6) unsigned").HasColumnName("resource_id");

                entity.Property(e => e.ProductId).HasColumnType("smallint(6) unsigned").HasColumnName("product_id");

                entity.Property(e => e.BasePrice).HasColumnType("int(11) unsigned").HasColumnName("base_price").HasDefaultValueSql("'1'");
            });

            modelBuilder.Entity<SkillsFarmingPatchDefinition>(entity =>
            {
                entity.HasKey(e => e.ObjectId).HasName("PRIMARY");

                entity.ToTable("skills_farming_patch_definitions");

                entity.HasCharSet("utf8").UseCollation("utf8_general_ci");

                entity.Property(e => e.ObjectId).HasColumnType("int(11) unsigned").ValueGeneratedNever().HasColumnName("object_id");

                entity.Property(e => e.Type).HasColumnType("enum('Herb','Tree','Bush','FruitTree','Flower','Hop','Allotment')").HasColumnName("type");
            });

            modelBuilder.Entity<SkillsFarmingSeedDefinition>(entity =>
            {
                entity.HasKey(e => e.ItemId).HasName("PRIMARY");

                entity.ToTable("skills_farming_seed_definitions");

                entity.HasCharSet("utf8").UseCollation("utf8_general_ci");

                entity.Property(e => e.ItemId).HasColumnType("smallint(6) unsigned").ValueGeneratedNever().HasColumnName("item_id");

                entity.Property(e => e.CycleTicks).HasColumnType("int(11) unsigned").HasColumnName("cycle_ticks").HasDefaultValueSql("'100'");

                entity.Property(e => e.HarvestExperience).HasPrecision(11, 2).HasColumnName("harvest_experience");

                entity.Property(e => e.MaxCycles).HasColumnType("tinyint(3) unsigned").HasColumnName("max_cycles").HasDefaultValueSql("'4'");

                entity.Property(e => e.MaximumProductCount).HasColumnType("int(11) unsigned").HasColumnName("maximum_product_count").HasDefaultValueSql("'1'");

                entity.Property(e => e.MinimumProductCount).HasColumnType("int(11) unsigned").HasColumnName("minimum_product_count").HasDefaultValueSql("'1'");

                entity.Property(e => e.PlantingExperience).HasPrecision(11, 2).HasColumnName("planting_experience");

                entity.Property(e => e.ProductId).HasColumnType("smallint(6) unsigned").HasColumnName("product_id");

                entity.Property(e => e.RequiredLevel).HasColumnType("tinyint(3) unsigned").HasColumnName("required_level").HasDefaultValueSql("'1'");

                entity.Property(e => e.Type).HasColumnType("enum('Herb','Bush','FruitTree','Flower','Hop','Tree','Allotment')").HasColumnName("type");

                entity.Property(e => e.VarpbitIndex).HasColumnType("tinyint(3)").HasColumnName("varpbit_index");
            });

            modelBuilder.Entity<SkillsFiremakingDefinition>(entity =>
            {
                entity.HasKey(e => e.ItemId).HasName("PRIMARY");

                entity.ToTable("skills_firemaking_definitions");

                entity.HasCharSet("utf8").UseCollation("utf8_general_ci");

                entity.Property(e => e.ItemId).HasColumnType("smallint(6) unsigned").ValueGeneratedNever().HasColumnName("item_id");

                entity.Property(e => e.Experience).HasPrecision(11, 2).HasColumnName("experience");

                entity.Property(e => e.FireObjectId).HasColumnType("int(11) unsigned").HasColumnName("fire_object_id").HasDefaultValueSql("'2732'");

                entity.Property(e => e.RequiredLevel).HasColumnType("tinyint(3) unsigned").HasColumnName("required_level").HasDefaultValueSql("'1'");

                entity.Property(e => e.Ticks).HasColumnType("int(11) unsigned").HasColumnName("ticks").HasDefaultValueSql("'100'");
            });

            modelBuilder.Entity<SkillsFishingFishDefinition>(entity =>
            {
                entity.HasKey(e => e.ItemId).HasName("PRIMARY");

                entity.ToTable("skills_fishing_fish_definitions");

                entity.HasCharSet("utf8").UseCollation("utf8_general_ci");

                entity.HasIndex(e => e.SpotId, "spot_id_foreign_key");

                entity.Property(e => e.ItemId).HasColumnType("smallint(6) unsigned").ValueGeneratedNever().HasColumnName("item_id");

                entity.Property(e => e.Experience).HasPrecision(11, 2).HasColumnName("experience");

                entity.Property(e => e.Probability).HasColumnType("decimal(11,3) unsigned").HasColumnName("probability");

                entity.Property(e => e.RequiredLevel).HasColumnType("tinyint(3) unsigned").HasColumnName("required_level").HasDefaultValueSql("'1'");

                entity.Property(e => e.SpotId).HasColumnType("int(11) unsigned").HasColumnName("spot_id");

                entity.HasOne(d => d.Spot)
                    .WithMany(p => p.SkillsFishingFishDefinitions)
                    .HasForeignKey(d => d.SpotId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("spot_id_foreign_key");
            });

            modelBuilder.Entity<SkillsFishingSpotDefinition>(entity =>
            {
                entity.ToTable("skills_fishing_spot_definitions");

                entity.HasCharSet("utf8").UseCollation("utf8_general_ci");

                entity.HasIndex(e => e.ToolId, "tool_id_foreign_key");

                entity.Property(e => e.Id).HasColumnType("int(11) unsigned").HasColumnName("id");

                entity.Property(e => e.BaitId).HasColumnType("smallint(6) unsigned").HasColumnName("bait_id");

                entity.Property(e => e.BaseCatchChance).HasPrecision(11, 3).HasColumnName("base_catch_chance");

                entity.Property(e => e.ClickType)
                    .HasColumnType("enum('Option1Click','Option2Click','Option3Click','Option4Click','Option5Click')")
                    .HasColumnName("click_type")
                    .HasDefaultValueSql("'Option1Click'");

                entity.Property(e => e.ExhaustChance).HasPrecision(11, 2).HasColumnName("exhaust_chance");

                entity.Property(e => e.MinimumLevel).HasColumnType("tinyint(3) unsigned").HasColumnName("minimum_level").HasDefaultValueSql("'1'");

                entity.Property(e => e.RespawnTime).HasColumnType("decimal(11,2) unsigned").HasColumnName("respawn_time").HasDefaultValueSql("'1.00'");

                entity.Property(e => e.ToolId).HasColumnType("smallint(6) unsigned").HasColumnName("tool_id").HasDefaultValueSql("'309'");

                entity.HasOne(d => d.Tool)
                    .WithMany(p => p.SkillsFishingSpotDefinitions)
                    .HasForeignKey(d => d.ToolId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("tool_id_foreign_key");
            });

            modelBuilder.Entity<SkillsFishingSpotNpcDefinition>(entity =>
            {
                entity.HasKey(e => new
                    {
                        e.SpotId, e.NpcId
                    })
                    .HasName("PRIMARY")
                    .HasAnnotation("MySql:IndexPrefixLength",
                        new[]
                        {
                            0, 0
                        });

                entity.ToTable("skills_fishing_spot_npc_definitions");

                entity.HasCharSet("utf8").UseCollation("utf8_general_ci");

                entity.Property(e => e.SpotId).HasColumnType("int(11) unsigned").HasColumnName("spot_id");

                entity.Property(e => e.NpcId).HasColumnType("smallint(6) unsigned").HasColumnName("npc_id");

                entity.HasOne(d => d.Spot)
                    .WithMany(p => p.SkillsFishingSpotNpcDefinitions)
                    .HasForeignKey(d => d.SpotId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("spot_id_foreign_key_11");
            });

            modelBuilder.Entity<SkillsFishingToolDefinition>(entity =>
            {
                entity.HasKey(e => e.ItemId).HasName("PRIMARY");

                entity.ToTable("skills_fishing_tool_definitions");

                entity.HasCharSet("utf8").UseCollation("utf8_general_ci");

                entity.Property(e => e.ItemId).HasColumnType("smallint(6) unsigned").ValueGeneratedNever().HasColumnName("item_id");

                entity.Property(e => e.CastAnimationId).HasColumnType("smallint(6) unsigned").HasColumnName("cast_animation_id");

                entity.Property(e => e.FishAnimationId).HasColumnType("smallint(6) unsigned").HasColumnName("fish_animation_id");
            });

            modelBuilder.Entity<SkillsFletchingDefinition>(entity =>
            {
                entity.HasKey(e => e.ResourceId).HasName("PRIMARY");

                entity.ToTable("skills_fletching_definitions");

                entity.HasCharSet("utf8").UseCollation("utf8_general_ci");

                entity.Property(e => e.ResourceId).HasColumnType("smallint(6) unsigned").ValueGeneratedNever().HasColumnName("resource_id");

                entity.Property(e => e.AnimationId).HasColumnType("smallint(6) unsigned").HasColumnName("animation_id");

                entity.Property(e => e.ProductCounts).HasMaxLength(255).HasColumnName("product_counts").HasDefaultValueSql("'1,1,1,1'");

                entity.Property(e => e.ProductExperiences).HasMaxLength(255).HasColumnName("product_experiences").HasDefaultValueSql("'0,0,0,0'");

                entity.Property(e => e.ProductIds).HasMaxLength(255).HasColumnName("product_ids").HasDefaultValueSql("'0,0,0,0'");

                entity.Property(e => e.RequiredLevels).HasMaxLength(255).HasColumnName("required_levels").HasDefaultValueSql("'1,1,1,1'");

                entity.Property(e => e.ToolId).HasColumnType("smallint(6) unsigned").HasColumnName("tool_id");
            });

            modelBuilder.Entity<SkillsHerbloreHerbDefinition>(entity =>
            {
                entity.HasKey(e => e.GrimyItemId).HasName("PRIMARY");

                entity.ToTable("skills_herblore_herb_definitions");

                entity.HasCharSet("utf8").UseCollation("utf8_general_ci");

                entity.Property(e => e.GrimyItemId).HasColumnType("smallint(6) unsigned").ValueGeneratedNever().HasColumnName("grimy_item_id");

                entity.Property(e => e.CleanItemId).HasColumnType("smallint(6) unsigned").HasColumnName("clean_item_id");

                entity.Property(e => e.Experience).HasPrecision(11, 2).HasColumnName("experience");

                entity.Property(e => e.RequiredLevel).HasColumnType("tinyint(3) unsigned").HasColumnName("required_level").HasDefaultValueSql("'1'");
            });

            modelBuilder.Entity<SkillsMagicCombatDefinition>(entity =>
            {
                entity.HasKey(e => e.ButtonId).HasName("PRIMARY");

                entity.ToTable("skills_magic_combat_definitions");

                entity.HasCharSet("utf8").UseCollation("utf8_general_ci");

                entity.Property(e => e.ButtonId).HasColumnType("smallint(6) unsigned").ValueGeneratedNever().HasColumnName("button_id");

                entity.Property(e => e.AutocastConfig).HasColumnType("int(11) unsigned").HasColumnName("autocast_config");

                entity.Property(e => e.BaseDamage).HasColumnType("int(11) unsigned").HasColumnName("base_damage");

                entity.Property(e => e.BaseExperience).HasPrecision(11, 2).HasColumnName("base_experience");

                entity.Property(e => e.CastAnimationId).HasColumnType("smallint(6)").HasColumnName("cast_animation_id").HasDefaultValueSql("'-1'");

                entity.Property(e => e.CastGraphicId).HasColumnType("smallint(6)").HasColumnName("cast_graphic_id").HasDefaultValueSql("'-1'");

                entity.Property(e => e.EndGraphicId).HasColumnType("smallint(6)").HasColumnName("end_graphic_id").HasDefaultValueSql("'-1'");

                entity.Property(e => e.ProjectileId).HasColumnType("smallint(6)").HasColumnName("projectile_id").HasDefaultValueSql("'-1'");

                entity.Property(e => e.RequiredLevel).HasColumnType("tinyint(3) unsigned").HasColumnName("required_level").HasDefaultValueSql("'1'");

                entity.Property(e => e.RequiredRunes).HasColumnType("text").HasColumnName("required_runes");

                entity.Property(e => e.RequiredRunesCounts).HasColumnType("text").HasColumnName("required_runes_counts");
            });

            modelBuilder.Entity<SkillsMagicEnchantDefinition>(entity =>
            {
                entity.HasKey(e => e.ButtonId).HasName("PRIMARY");

                entity.ToTable("skills_magic_enchant_definitions");

                entity.HasCharSet("utf8").UseCollation("utf8_general_ci");

                entity.Property(e => e.ButtonId).HasColumnType("smallint(6) unsigned").ValueGeneratedNever().HasColumnName("button_id");

                entity.Property(e => e.Experience).HasPrecision(11, 2).HasColumnName("experience");

                entity.Property(e => e.GraphicId).HasColumnType("smallint(6) unsigned").HasColumnName("graphic_id");

                entity.Property(e => e.RequiredLevel).HasColumnType("tinyint(3) unsigned").HasColumnName("required_level").HasDefaultValueSql("'1'");

                entity.Property(e => e.RequiredRunes).HasColumnType("text").HasColumnName("required_runes");

                entity.Property(e => e.RequiredRunesCounts).HasColumnType("text").HasColumnName("required_runes_counts");
            });

            modelBuilder.Entity<SkillsMagicEnchantProduct>(entity =>
            {
                entity.HasKey(e => e.ResourceId).HasName("PRIMARY");

                entity.ToTable("skills_magic_enchant_products");

                entity.HasCharSet("utf8").UseCollation("utf8_general_ci");

                entity.HasIndex(e => e.ButtonId, "child_id_foreign_key");

                entity.Property(e => e.ResourceId).HasColumnType("smallint(6) unsigned").ValueGeneratedNever().HasColumnName("resource_id");

                entity.Property(e => e.ButtonId).HasColumnType("smallint(6) unsigned").HasColumnName("button_id");

                entity.Property(e => e.ProductId).HasColumnType("smallint(6) unsigned").HasColumnName("product_id");

                entity.HasOne(d => d.Button)
                    .WithMany(p => p.SkillsMagicEnchantProducts)
                    .HasForeignKey(d => d.ButtonId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("skills_magic_enchant_products_ibfk_1");
            });

            modelBuilder.Entity<SkillsMagicTeleportDefinition>(entity =>
            {
                entity.HasKey(e => new
                    {
                        e.ButtonId, e.SpellBook
                    })
                    .HasName("PRIMARY")
                    .HasAnnotation("MySql:IndexPrefixLength",
                        new[]
                        {
                            0, 0
                        });

                entity.ToTable("skills_magic_teleport_definitions");

                entity.HasCharSet("utf8").UseCollation("utf8_general_ci");

                entity.Property(e => e.ButtonId).HasColumnName("button_id");

                entity.Property(e => e.SpellBook)
                    .HasColumnType("enum('StandartBook','AncientBook','LunarBook','DungeoneeringBook')")
                    .HasColumnName("spell_book")
                    .HasDefaultValueSql("'StandartBook'");

                entity.Property(e => e.CoordX).HasColumnName("coord_x").HasDefaultValueSql("'3222'");

                entity.Property(e => e.CoordY).HasColumnName("coord_y").HasDefaultValueSql("'3222'");

                entity.Property(e => e.CoordZ).HasColumnName("coord_z");

                entity.Property(e => e.Distance).HasColumnName("distance");

                entity.Property(e => e.Experience).HasPrecision(11, 2).HasColumnName("experience");

                entity.Property(e => e.RequiredLevel).HasColumnName("required_level").HasDefaultValueSql("'1'");

                entity.Property(e => e.RequiredRunes).HasColumnName("required_runes").UseCollation("utf8mb4_0900_ai_ci").HasCharSet("utf8mb4");

                entity.Property(e => e.RequiredRunesCount).HasColumnName("required_runes_count").UseCollation("utf8mb4_0900_ai_ci").HasCharSet("utf8mb4");
            });

            modelBuilder.Entity<SkillsMiningOreDefinition>(entity =>
            {
                entity.HasKey(e => e.ItemId).HasName("PRIMARY");

                entity.ToTable("skills_mining_ore_definitions");

                entity.HasCharSet("utf8").UseCollation("utf8_general_ci");

                entity.Property(e => e.ItemId).HasColumnType("smallint(6) unsigned").ValueGeneratedNever().HasColumnName("item_id");

                entity.Property(e => e.BaseHarvestChance).HasColumnType("decimal(11,3) unsigned").HasColumnName("base_harvest_chance");

                entity.Property(e => e.ExhaustChance).HasColumnType("decimal(11,3) unsigned").HasColumnName("exhaust_chance");

                entity.Property(e => e.Experience).HasPrecision(11, 2).HasColumnName("experience");

                entity.Property(e => e.RequiredLevel).HasColumnType("tinyint(3) unsigned").HasColumnName("required_level").HasDefaultValueSql("'1'");

                entity.Property(e => e.RespawnTime).HasPrecision(11, 3).HasColumnName("respawn_time").HasDefaultValueSql("'1.000'");
            });

            modelBuilder.Entity<SkillsMiningPickaxeDefinition>(entity =>
            {
                entity.HasKey(e => new
                    {
                        e.Type, e.ItemId
                    })
                    .HasName("PRIMARY")
                    .HasAnnotation("MySql:IndexPrefixLength",
                        new[]
                        {
                            0, 0
                        });

                entity.ToTable("skills_mining_pickaxe_definitions");

                entity.HasCharSet("utf8").UseCollation("utf8_general_ci");

                entity.Property(e => e.Type).HasColumnType("enum('Dragon','Rune','Adamant','Mithril','Steel','Iron','Bronze')").HasColumnName("type");

                entity.Property(e => e.ItemId).HasColumnType("smallint(6) unsigned").HasColumnName("item_id");

                entity.Property(e => e.AnimationId).HasColumnType("smallint(6) unsigned").HasColumnName("animation_id");

                entity.Property(e => e.BaseHarvestChance).HasColumnType("decimal(11,3) unsigned").HasColumnName("base_harvest_chance");

                entity.Property(e => e.RequiredLevel).HasColumnType("tinyint(3) unsigned").HasColumnName("required_level").HasDefaultValueSql("'1'");
            });

            modelBuilder.Entity<SkillsMiningRockDefinition>(entity =>
            {
                entity.HasKey(e => e.RockId).HasName("PRIMARY");

                entity.ToTable("skills_mining_rock_definitions");

                entity.HasCharSet("utf8").UseCollation("utf8_general_ci");

                entity.HasIndex(e => e.OreId, "ore_foreign_key");

                entity.Property(e => e.RockId).HasColumnType("int(11) unsigned").ValueGeneratedNever().HasColumnName("rock_id");

                entity.Property(e => e.ExhaustRockId).HasColumnType("int(11) unsigned").HasColumnName("exhaust_rock_id");

                entity.Property(e => e.OreId).HasColumnType("smallint(6) unsigned").HasColumnName("ore_id");

                entity.HasOne(d => d.Ore)
                    .WithMany(p => p.SkillsMiningRockDefinitions)
                    .HasForeignKey(d => d.OreId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("skills_mining_rock_definitions_ibfk_1");

                entity.HasOne(d => d.Rock)
                    .WithOne(p => p.SkillsMiningRockDefinition)
                    .HasForeignKey<SkillsMiningRockDefinition>(d => d.RockId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("rock_id_foreign_key");
            });

            modelBuilder.Entity<SkillsPrayerDefinition>(entity =>
            {
                entity.HasKey(e => e.ItemId).HasName("PRIMARY");

                entity.ToTable("skills_prayer_definitions");

                entity.HasCharSet("utf8").UseCollation("utf8_general_ci");

                entity.Property(e => e.ItemId).HasColumnType("smallint(6) unsigned").ValueGeneratedNever().HasColumnName("item_id");

                entity.Property(e => e.Experience).HasColumnType("decimal(11,2) unsigned").HasColumnName("experience");

                entity.Property(e => e.Type).HasColumnType("enum('Ashes','Bones')").HasColumnName("type");
            });

            modelBuilder.Entity<SkillsRunecraftingDefinition>(entity =>
            {
                entity.HasKey(e => e.AltarId).HasName("PRIMARY");

                entity.ToTable("skills_runecrafting_definitions");

                entity.HasCharSet("utf8").UseCollation("utf8_general_ci");

                entity.Property(e => e.AltarId).HasColumnType("int(11) unsigned").ValueGeneratedNever().HasColumnName("altar_id");

                entity.Property(e => e.AltarLocation).HasMaxLength(12).HasColumnName("altar_location").HasDefaultValueSql("'3222,3222,0'");

                entity.Property(e => e.Experience).HasPrecision(11, 2).HasColumnName("experience");

                entity.Property(e => e.LevelCountMultipliers).HasMaxLength(32).HasColumnName("level_count_multipliers").HasDefaultValueSql("'-1'");

                entity.Property(e => e.PortalId).HasColumnType("int(11) unsigned").HasColumnName("portal_id");

                entity.Property(e => e.RequiredLevel).HasColumnType("tinyint(3) unsigned").HasColumnName("required_level").HasDefaultValueSql("'1'");

                entity.Property(e => e.RiftId).HasColumnType("int(11) unsigned").HasColumnName("rift_id");

                entity.Property(e => e.RiftLocation).HasMaxLength(12).HasColumnName("rift_location").HasDefaultValueSql("'3222,3222,0'");

                entity.Property(e => e.RuinId).HasColumnType("int(11) unsigned").HasColumnName("ruin_id");

                entity.Property(e => e.RuinLocation).HasMaxLength(12).HasColumnName("ruin_location").HasDefaultValueSql("'3222,3222,0'");

                entity.Property(e => e.RuneId).HasColumnType("smallint(6) unsigned").HasColumnName("rune_id");

                entity.Property(e => e.TalismanId).HasColumnType("smallint(6) unsigned").HasColumnName("talisman_id");

                entity.Property(e => e.TiaraId).HasColumnType("smallint(6) unsigned").HasColumnName("tiara_id");
            });

            modelBuilder.Entity<SkillsSlayerMasterDefinition>(entity =>
            {
                entity.HasKey(e => e.NpcId).HasName("PRIMARY");

                entity.ToTable("skills_slayer_master_definitions");

                entity.HasCharSet("latin1").UseCollation("latin1_swedish_ci");

                entity.Property(e => e.NpcId).HasColumnType("smallint(6) unsigned").ValueGeneratedNever().HasColumnName("npc_id");

                entity.Property(e => e.BaseSlayerRewardPoints).HasColumnType("int(11) unsigned").HasColumnName("base_slayer_reward_points");

                entity.Property(e => e.Name).HasMaxLength(24).HasColumnName("name");
            });

            modelBuilder.Entity<SkillsSlayerTaskDefinition>(entity =>
            {
                entity.ToTable("skills_slayer_task_definitions");

                entity.HasCharSet("latin1").UseCollation("latin1_swedish_ci");

                entity.HasIndex(e => e.SlayerMasterId, "slayer_master_foreign_key");

                entity.Property(e => e.Id).HasColumnType("smallint(6) unsigned").HasColumnName("id");

                entity.Property(e => e.CoinCount).HasColumnType("int(11) unsigned").HasColumnName("coin_count");

                entity.Property(e => e.CombatRequirement).HasColumnType("tinyint(3) unsigned").HasColumnName("combat_requirement").HasDefaultValueSql("'3'");

                entity.Property(e => e.LevelRequirement).HasColumnType("tinyint(3) unsigned").HasColumnName("level_requirement").HasDefaultValueSql("'1'");

                entity.Property(e => e.MaximumCount).HasColumnType("int(6)").HasColumnName("maximum_count").HasDefaultValueSql("'1'");

                entity.Property(e => e.MinimumCount).HasColumnType("int(11)").HasColumnName("minimum_count").HasDefaultValueSql("'1'");

                entity.Property(e => e.Name).HasMaxLength(32).HasColumnName("name").HasDefaultValueSql("''");

                entity.Property(e => e.NpcIds)
                    .HasColumnType("text")
                    .HasColumnName("npc_ids")
                    .HasComment("The NPC IDs that can be slayed for slayer experience.");

                entity.Property(e => e.SlayerMasterId).HasColumnType("smallint(6) unsigned").HasColumnName("slayer_master_id");

                entity.HasOne(d => d.SlayerMaster)
                    .WithMany(p => p.SkillsSlayerTaskDefinitions)
                    .HasForeignKey(d => d.SlayerMasterId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("slayer_master_foreign_key");
            });

            modelBuilder.Entity<SkillsSummoningDefinition>(entity =>
            {
                entity.HasKey(e => e.NpcId).HasName("PRIMARY");

                entity.ToTable("skills_summoning_definitions");

                entity.HasCharSet("utf8").UseCollation("utf8_general_ci");

                entity.Property(e => e.NpcId).HasColumnType("smallint(6) unsigned").ValueGeneratedNever().HasColumnName("npc_id");

                entity.Property(e => e.ConfigId).HasColumnType("smallint(6) unsigned").HasColumnName("config_id");

                entity.Property(e => e.CreatePouchExperience).HasPrecision(11, 2).HasColumnName("create_pouch_experience");

                entity.Property(e => e.PouchId).HasColumnType("smallint(6) unsigned").HasColumnName("pouch_id");

                entity.Property(e => e.ScrollExperience).HasPrecision(11, 2).HasColumnName("scroll_experience");

                entity.Property(e => e.ScrollId).HasColumnType("smallint(6) unsigned").HasColumnName("scroll_id");

                entity.Property(e => e.SummonExperience).HasPrecision(11, 2).HasColumnName("summon_experience");

                entity.Property(e => e.SummonLevel).HasColumnType("tinyint(3) unsigned").HasColumnName("summon_level").HasDefaultValueSql("'1'");

                entity.Property(e => e.SummonSpawnCost).HasColumnType("tinyint(3) unsigned").HasColumnName("summon_spawn_cost");

                entity.Property(e => e.Ticks).HasColumnType("int(11) unsigned").HasColumnName("ticks").HasDefaultValueSql("'100'");
            });

            modelBuilder.Entity<SkillsWoodcuttingHatchetDefinition>(entity =>
            {
                entity.HasKey(e => new
                    {
                        e.Type, e.ItemId
                    })
                    .HasName("PRIMARY")
                    .HasAnnotation("MySql:IndexPrefixLength",
                        new[]
                        {
                            0, 0
                        });

                entity.ToTable("skills_woodcutting_hatchet_definitions");

                entity.HasCharSet("utf8").UseCollation("utf8_general_ci");

                entity.Property(e => e.Type).HasColumnType("enum('Bronze','Iron','Black','Steel','Mithril','Adamant','Rune','Dragon')").HasColumnName("type");

                entity.Property(e => e.ItemId).HasColumnType("smallint(6) unsigned").HasColumnName("item_id");

                entity.Property(e => e.BaseHarvestChance).HasColumnType("decimal(11,3) unsigned").HasColumnName("base_harvest_chance");

                entity.Property(e => e.CanoueAnimationId).HasColumnType("smallint(6) unsigned").HasColumnName("canoue_animation_id");

                entity.Property(e => e.ChopAnimationId).HasColumnType("smallint(6) unsigned").HasColumnName("chop_animation_id");

                entity.Property(e => e.RequiredLevel).HasColumnType("tinyint(3) unsigned").HasColumnName("required_level").HasDefaultValueSql("'1'");
            });

            modelBuilder.Entity<SkillsWoodcuttingLogDefinition>(entity =>
            {
                entity.HasKey(e => e.ItemId).HasName("PRIMARY");

                entity.ToTable("skills_woodcutting_log_definitions");

                entity.HasCharSet("utf8").UseCollation("utf8_general_ci");

                entity.Property(e => e.ItemId).HasColumnType("smallint(6) unsigned").ValueGeneratedNever().HasColumnName("item_id");

                entity.Property(e => e.BaseHarvestChance).HasPrecision(11, 3).HasColumnName("base_harvest_chance");

                entity.Property(e => e.Experience).HasPrecision(11, 2).HasColumnName("experience");

                entity.Property(e => e.FallChance).HasPrecision(11, 3).HasColumnName("fall_chance").HasDefaultValueSql("'0.150'");

                entity.Property(e => e.RequiredLevel).HasColumnType("tinyint(3) unsigned").HasColumnName("required_level").HasDefaultValueSql("'1'");

                entity.Property(e => e.RespawnTime)
                    .HasPrecision(11, 3)
                    .HasColumnName("respawn_time")
                    .HasDefaultValueSql("'1.000'")
                    .HasComment("Respawn time in minutes");
            });

            modelBuilder.Entity<SkillsWoodcuttingTreeDefinition>(entity =>
            {
                entity.HasKey(e => e.TreeId).HasName("PRIMARY");

                entity.ToTable("skills_woodcutting_tree_definitions");

                entity.HasCharSet("utf8").UseCollation("utf8_general_ci");

                entity.HasIndex(e => e.LogId, "skills_woodcutting_log_id");

                entity.Property(e => e.TreeId).HasColumnType("int(11) unsigned").ValueGeneratedNever().HasColumnName("tree_id");

                entity.Property(e => e.LogId).HasColumnType("smallint(6) unsigned").HasColumnName("log_id");

                entity.Property(e => e.StumpId).HasColumnType("int(11) unsigned").HasColumnName("stump_id");

                entity.HasOne(d => d.Log)
                    .WithMany(p => p.SkillsWoodcuttingTreeDefinitions)
                    .HasForeignKey(d => d.LogId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("skills_woodcutting_log_id");

                entity.HasOne(d => d.Tree)
                    .WithOne(p => p.SkillsWoodcuttingTreeDefinition)
                    .HasForeignKey<SkillsWoodcuttingTreeDefinition>(d => d.TreeId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("skills_woodcutting_tree_id");
            });

            modelBuilder.Entity<World>(entity =>
            {
                entity.ToTable("worlds");

                entity.HasCharSet("latin1").UseCollation("latin1_swedish_ci");

                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.AccountCreationEnabled).HasColumnName("account_creation_enabled").HasDefaultValueSql("'1'");

                entity.Property(e => e.Country).HasColumnName("country").HasDefaultValueSql("'12'");

                entity.Property(e => e.DirectLoginEnabled).HasColumnName("direct_login_enabled");

                entity.Property(e => e.GameAdminsOnly).HasColumnName("game_admins_only");

                entity.Property(e => e.HighRisk).HasColumnName("high_risk");

                entity.Property(e => e.Highlight).HasColumnName("highlight");

                entity.Property(e => e.LobbyAdminsOnly).HasColumnName("lobby_admins_only");

                entity.Property(e => e.LootShareAllowed).HasColumnName("loot_share_allowed");

                entity.Property(e => e.MembersOnly).HasColumnName("members_only").HasDefaultValueSql("'1'");

                entity.Property(e => e.Name).HasColumnType("tinytext").HasColumnName("name");

                entity.Property(e => e.QuickChatAllowed).HasColumnName("quick_chat_allowed");

                entity.Property(e => e.Region).HasMaxLength(5).HasColumnName("region").HasDefaultValueSql("'PT'");

                entity.Property(e => e.SkillReq).HasColumnName("skill_req");
            });

            modelBuilder.Entity<WorldConfiguration>(entity =>
            {
                entity.ToTable("world_configurations");

                entity.Property(e => e.Id).HasColumnName("id").UseCollation("latin1_swedish_ci").HasCharSet("latin1");

                entity.Property(e => e.Value).HasColumnName("value");
            });
        }
    }
}