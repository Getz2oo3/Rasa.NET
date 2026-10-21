using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Services.Preloader.Missions.Pravus
{
    public static partial class PravusOperationV1
    {
        private static readonly uint[] PreviousPools = { 560013, 560014, 560015, 560016, 640101, 640102, 640103, 640201, 640202, 640203, 640204 };
        private static void World(MigrationBuilder m)
        {
            foreach (var id in PreviousPools)
                m.Sql($"UPDATE spawnpool SET mode=2 WHERE id={id} AND map_context_id=1430;");
            // Clone known working templates, never alter an already-applied population migration.
            Clone(m, 640301, 630100, "Pravus Frontlines Forean Machina", 6236, 10, 700);
            Clone(m, 640302, 630100, "Pravus production Forean Machina", 6236, 10, 700);
            Clone(m, 640303, 560001, "Pravus key-bearing Thrax guard", 0, 10, 550);
            Clone(m, 640304, 560001, "Pravus Thrax soldier", 0, 10, 550);
            Clone(m, 640305, 560002, "Pravus Thrax grenadier", 0, 10, 550);
            Clone(m, 640306, 640101, "Pravus passage Linker", 0, 10, 750);
            Clone(m, 640307, 560001, "Pravus alarm Juggernaut", 7722, 11, 3500);
            m.Sql("UPDATE creature SET name_id=0, action1=640307, action2=640308, action3=0, action4=0, action5=0, action6=0, action7=0, action8=0 WHERE id=640307;");
            m.Sql("DELETE FROM creature_appearance WHERE id=640307;");
            Insert(m, "creature_appearance", new[] { "id", "slot_id", "Class_id", "color" },
                new object[] { 640307U, 13U, 8084U, 0U });
            var actionColumns = new[] { "id", "description", "action_id", "action_arg_id", "range_min", "range_max", "cooldown", "windup", "min_damage", "max_damage", "damage_type" };
            Insert(m, "creature_action", actionColumns, new object[] { 640307U, "Pravus Juggernaut guns", 428U, 1U, 0.0, 40.0, 2500U, 900U, 20U, 30U, 1U });
            Insert(m, "creature_action", actionColumns, new object[] { 640308U, "Pravus Juggernaut eyebeam", 243U, 1U, 0.0, 45.0, 16000U, 2200U, 40U, 55U, 1U });
            Clone(m, 640309, 39, "Pravus AFS Frontlines support", 0, 10, 1800);
            Clone(m, 640311, 640002, "Information Spec. Johnson - Pravus", 0, 10, 1000);
            m.Sql("UPDATE creature SET name_id=206 WHERE id=640311;");
            Clone(m, 640312, 640001, "Forean Ranger Maulis - Pravus", 0, 10, 1000);
            m.Sql("UPDATE creature SET name_id=5286 WHERE id=640312;");
            foreach (var (id, package) in new[] { (640001U,420U), (640002U,105U), (640003U,104U), (640311U,106U), (640312U,468U) })
                Insert(m,"npc_package", new[] { "id", "package_id", "comment" }, new object[] { id, package, "Pravus operation contact" });

            Pool(m,640401,162,41.94,-8,6,640303,3,640305,1);
            Pool(m,640402,223.9844,9.7033,-7.7422,6,640304,3,640305,1);
            Pool(m,640403,139.7,17.33,98.39,5,640306,3);
            Pool(m,640404,-60,25.937,115,10,640302,4);
            Pool(m,640405,-85,38.461,-80,6,640302,4);
            Pool(m,640406,0,30.277,245,8,640302,3);
            Pool(m,640407,52.2,39.14,-193.4,8,640304,3,640305,1);
            Pool(m,640408,144.22,10.52,151.4,6,640304,3,640305,1,scripted:true);
            Pool(m,640409,263.77,8.74,128.29,7,640304,3,640305,1,scripted:true);
            Pool(m,640410,263.77,8.74,207.84,7,640304,3,640305,1,scripted:true);
            Pool(m,640411,310,9.819,58,0,640312,1);
            Pool(m,640412,-83.2,34.490,-120.4,0,640311,1);
            Pool(m,640413,-126,27.64,14,4,640309,3);
            Pool(m,640414,215,9,120,0,640001,1,scripted:true);

            // Every hostile definition has ordinary loot, independent of its personal quest drops.
            // Reuse supported level-band profiles; no fabricated loot implementation.
            foreach (var id in new uint[] { 640301,640302,640303,640304,640305,640306,640307,560001,560002,640101,640201 })
            {
                var profile = id == 640307 ? "wilderness_v1_thrax_10_14_boss" : "wilderness_v1_thrax_10_14";
                Insert(m,"creature_loot_binding", new[] { "map_context_id", "creature_id", "profile_key" },
                    new object[] { 1430U, id, profile });
            }
            Insert(m,"loot_map_policy", new[] { "map_context_id", "require_profiles" }, new object[] { 1430U, true });
        }

        private static void Clone(MigrationBuilder m, uint id, uint source, string name, uint classId, uint level, uint health)
        {
            m.Sql($"INSERT INTO creature (id,comment,class_id,faction,level,max_hp,name_id,run_speed,walk_speed,action1,action2,action3,action4,action5,action6,action7,action8) " +
                $"SELECT {id},'{name}',{(classId == 0 ? "class_id" : classId.ToString())},faction,{level},{health},name_id,run_speed,walk_speed,action1,action2,action3,action4,action5,action6,action7,action8 FROM creature WHERE id={source};");
            m.Sql($"INSERT INTO creature_stat (id,body,mind,spirit,health,armor) SELECT {id},body,mind,spirit,{health},armor FROM creature_stat WHERE id={source};");
            m.Sql($"INSERT INTO creature_appearance (id,slot_id,Class_id,color) SELECT {id},slot_id,Class_id,color FROM creature_appearance WHERE id={source};");
        }

        private static void Pool(MigrationBuilder m,uint id,double x,double y,double z,double radius,
            uint creature,int count,uint second=0,int secondCount=0,bool scripted=false)
        {
            Insert(m,"spawnpool",new[] { "id","mode","anim_type","respown_time","pos_x","pos_y","pos_z","rotation","map_context_id",
                "creature_1_Id","creature_1_min_count","creature_1_max_count","creature_2_Id","creature_2_min_count","creature_2_max_count",
                "creature_3_Id","creature_3_min_count","creature_3_max_count","creature_4_Id","creature_4_min_count","creature_4_max_count",
                "creature_5_Id","creature_5_min_count","creature_5_max_count","creature_6_Id","creature_6_min_count","creature_6_max_count","radius" },
                new object[] { id,(short)(scripted?2:0),(short)0,1800U,x,y,z,0.0,1430U,
                    creature,(byte)count,(byte)count,second,(byte)secondCount,(byte)secondCount,
                    0U,(byte)0,(byte)0,0U,(byte)0,(byte)0,0U,(byte)0,(byte)0,0U,(byte)0,(byte)0,radius });
        }

        private static void RemoveWorld(MigrationBuilder m)
        {
            m.Sql("DELETE FROM loot_map_policy WHERE map_context_id=1430;");
            m.Sql("DELETE FROM creature_loot_binding WHERE map_context_id=1430 AND creature_id IN (640301,640302,640303,640304,640305,640306,640307,560001,560002,640101,640201);");
            foreach(var id in new uint[] {640001,640002,640003,640311,640312}) m.Sql($"DELETE FROM npc_package WHERE id={id};");
            m.Sql("DELETE FROM spawnpool WHERE id BETWEEN 640401 AND 640414;");
            m.Sql("DELETE FROM creature_action WHERE id IN (640307,640308);");
            foreach(var id in new uint[] {640301,640302,640303,640304,640305,640306,640307,640309,640311,640312})
            {
                m.Sql($"DELETE FROM creature_appearance WHERE id={id};");
                m.Sql($"DELETE FROM creature_stat WHERE id={id};");
                m.Sql($"DELETE FROM creature WHERE id={id};");
            }
            foreach(var id in PreviousPools) m.Sql($"UPDATE spawnpool SET mode=0 WHERE id={id} AND map_context_id=1430;");
        }

        private static void Insert(MigrationBuilder m, string table, string[] columns, object[] values)
        {
            var sqlite = m.ActiveProvider == "Microsoft.EntityFrameworkCore.Sqlite";
            var types = new string[values.Length];
            for (var i = 0; i < values.Length; i++)
                types[i] = values[i] switch
                {
                    string => sqlite ? "TEXT" : "varchar(255)",
                    double => sqlite ? "REAL" : "double",
                    bool => sqlite ? "INTEGER" : "bit",
                    byte => sqlite ? "INTEGER" : "tinyint unsigned",
                    short => sqlite ? "INTEGER" : "smallint",
                    _ => sqlite ? "INTEGER" : "int unsigned"
                };
            m.InsertData(table: table, columns: columns, columnTypes: types, values: values);
        }
    }
}
