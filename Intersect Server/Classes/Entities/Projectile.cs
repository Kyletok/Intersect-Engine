﻿/*
    Intersect Game Engine (Server)
    Copyright (C) 2015  JC Snider, Joe Bridges
    
    Website: http://ascensiongamedev.com
    Contact Email: admin@ascensiongamedev.com 

    This program is free software; you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation; either version 2 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License along
    with this program; if not, write to the Free Software Foundation, Inc.,
    51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA.
*/
using Intersect_Server.Classes;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Intersect_Server.Classes
{
    public class Projectile : Entity
    {
        public Entity Owner;
        private ProjectileStruct MyBase;
        private int ProjectileNum = 0;
        private int Quantity = 0;
        private long SpawnTime = 0;
        public int Target = -1;
        private int IsSpell = -1;
        private int _spawnCount = 0;
        private int _totalSpawns = 0;
        private int _spawnedAmount = 0;

        // Individual Spawns
        public ProjectileSpawns[] Spawns;

        public Projectile(int index, Entity owner, int projectileNum, int Map, int X, int Y, int Z, int Direction, int isSpell = -1, int target = 0) : base(index)
        {
            ProjectileNum = projectileNum;
            MyBase = Globals.GameProjectiles[ProjectileNum];
            MyName = MyBase.Name;
            Owner = owner;
            Stat = owner.Stat;
            Vital[(int)Enums.Vitals.Health] = 1;
            MaxVital[(int)Enums.Vitals.Health] = 1;
            CurrentMap = Map;
            CurrentX = X;
            CurrentY = Y;
            CurrentZ = Z;
            Dir = Direction;
            IsSpell = isSpell;

            if (MyBase.Homing == true)
            {
                Target = target;
            }

            Passable = 1;
            HideName = 1;
            for (int x = 0; x < ProjectileStruct.SpawnLocationsWidth; x++)
            {
                for (int y = 0; y < ProjectileStruct.SpawnLocationsWidth; y++)
                {
                    for (int d = 0; d < ProjectileStruct.MaxProjectileDirections; d++)
                    {
                        if (MyBase.SpawnLocations[x, y].Directions[d] == true)
                        {
                            _totalSpawns++;
                        }
                    }
                }
            }
            Spawns = new ProjectileSpawns[_totalSpawns];
        }

        private void AddProjectileSpawns()
        {
            ProjectileStruct myBase = Globals.GameProjectiles[ProjectileNum];

            for (int x = 0; x < ProjectileStruct.SpawnLocationsWidth; x++)
            {
                for (int y = 0; y < ProjectileStruct.SpawnLocationsWidth; y++)
                {
                    for (int d = 0; d < ProjectileStruct.MaxProjectileDirections; d++)
                    {
                        if (myBase.SpawnLocations[x, y].Directions[d] == true)
                        {
                            ProjectileSpawns s = new ProjectileSpawns(FindProjectileRotationDir(Dir, d), CurrentX + FindProjectileRotationX(Dir, x - 2, y - 2), CurrentY + FindProjectileRotationY(Dir, x - 2, y - 2), CurrentZ, CurrentMap,myBase, MyIndex);
                            Spawns[_spawnedAmount] = s;
                            _spawnedAmount++;
                            _spawnCount++;
                        }
                    }
                }
            }
            Quantity++;
            SpawnTime = Environment.TickCount + MyBase.Delay;
        }

        private int FindProjectileRotationX(int direction, int x, int y)
        {
            switch (direction)
            {
                case 0: //Up
                    return x;
                case 1: //Down
                    return -x;
                case 2: //Left
                    return y;
                case 3: //Right
                    return -y;
                default:
                    return x;
            }
        }

        private int FindProjectileRotationY(int direction, int x, int y)
        {
            switch (direction)
            {
                case 0: //Up
                    return y;
                case 1: //Down
                    return -y;
                case 2: //Left
                    return -x;
                case 3: //Right
                    return x;
                default:
                    return y;
            }
        }

        private int FindProjectileRotationDir(int entityDir, int projectionDir)
        {
            switch (entityDir)
            {
                case 0: //Up
                    return projectionDir;
                case 1: //Down
                    switch (projectionDir)
                    {
                        case 0: //Up
                            return 1;
                        case 1: //Down
                            return 0;
                        case 2: //Left
                            return 3;
                        case 3: //Right
                            return 2;
                        case 4: //UpLeft
                            return 7;
                        case 5: //UpRight
                            return 6;
                        case 6: //DownLeft
                            return 5;
                        case 7: //DownRight
                            return 4;
                        default:
                            return projectionDir;
                    }
                case 2: //Left
                    switch (projectionDir)
                    {
                        case 0: //Up
                            return 2;
                        case 1: //Down
                            return 3;
                        case 2: //Left
                            return 1;
                        case 3: //Right
                            return 0;
                        case 4: //UpLeft
                            return 6;
                        case 5: //UpRight
                            return 4;
                        case 6: //DownLeft
                            return 7;
                        case 7: //DownRight
                            return 5;
                        default:
                            return projectionDir;
                    }
                case 3: //Right
                    switch (projectionDir)
                    {
                        case 0: //Up
                            return 3;
                        case 1: //Down
                            return 2;
                        case 2: //Left
                            return 0;
                        case 3: //Right
                            return 1;
                        case 4: //UpLeft
                            return 5;
                        case 5: //UpRight
                            return 7;
                        case 6: //DownLeft
                            return 4;
                        case 7: //DownRight
                            return 6;
                        default:
                            return projectionDir;
                    }
                default:
                    return projectionDir;
            }
        }

        public void Update()
        {
            if (Quantity < MyBase.Quantity && Environment.TickCount > SpawnTime)
            {
                AddProjectileSpawns();
            }
            CheckForCollision();
        }

        private int GetRangeX(int direction, int range)
        {
            //Left, UpLeft, DownLeft
            if (direction == 2 || direction == 4 || direction == 6)
            {
                return -range;
            }
            //Right, UpRight, DownRight
            else if (direction == 3 || direction == 5 || direction == 7)
            {
                return range;
            }
            //Up and Down
            else
            {
                return 0;
            }
        }

        private int GetRangeY(int direction, int range)
        {
            //Up, UpLeft, UpRight
            if (direction == 0 || direction == 4 || direction == 5)
            {
                return -range;
            }
            //Down, DownLeft, DownRight
            else if (direction == 1 || direction == 6 || direction == 7)
            {
                return range;
            }
            //Left and Right
            else
            {
                return 0;
            }
        }

        public void CheckForCollision()
        {
            if (_spawnCount != 0 || Quantity < MyBase.Quantity)
            {
                for (int i = 0; i < _spawnedAmount; i++)
                {
                    if (Spawns[i] != null && Environment.TickCount > Spawns[i].TransmittionTimer)
                    {
                        Spawns[i].Distance++;
                        bool killSpawn = false;
                        int newx = Spawns[i].X + GetRangeX(Spawns[i].Dir, 1);
                        int newy = Spawns[i].Y + GetRangeY(Spawns[i].Dir, 1);
                        int newmap = Spawns[i].Map;

                        if (newx < 0)
                        {
                            if (Globals.GameMaps[Spawns[i].Map].Left > -1 && Globals.GameMaps[Globals.GameMaps[Spawns[i].Map].Left] != null)
                            {
                                newmap = Globals.GameMaps[Spawns[i].Map].Left;
                                newx = Globals.MapWidth - 1;
                            }
                            else
                            {
                                killSpawn = true;
                            }
                        }
                        if (newx > Globals.MapWidth - 1)
                        {
                            if (Globals.GameMaps[Spawns[i].Map].Right > -1 && Globals.GameMaps[Globals.GameMaps[Spawns[i].Map].Right] != null)
                            {
                                newmap = Globals.GameMaps[Spawns[i].Map].Right;
                                newx = 0;
                            }
                            else
                            {
                                killSpawn = true;
                            }
                        }
                        if (newy < 0)
                        {
                            if (Globals.GameMaps[Spawns[i].Map].Up > -1 && Globals.GameMaps[Globals.GameMaps[Spawns[i].Map].Up] != null)
                            {
                                newmap = Globals.GameMaps[Spawns[i].Map].Up;
                                newy = Globals.MapHeight - 1;
                            }
                            else
                            {
                                killSpawn = true;
                            }
                        }
                        if (newy > Globals.MapHeight - 1)
                        {
                            if (Globals.GameMaps[Spawns[i].Map].Down > -1 && Globals.GameMaps[Globals.GameMaps[Spawns[i].Map].Down] != null)
                            {
                                newmap = Globals.GameMaps[Spawns[i].Map].Down;
                                newy = 0;
                            }
                            else
                            {
                                killSpawn = true;
                            }
                        }

                        if (killSpawn)
                        {
                            Spawns[i].Dispose(i);
                            Spawns[i] = null;
                            _spawnCount--;
                            continue;
                        }

                        Entity TempEntity = new Entity(0);
                        TempEntity.CurrentX = Spawns[i].X;
                        TempEntity.CurrentY = Spawns[i].Y;
                        TempEntity.CurrentZ = Spawns[i].Z;
                        TempEntity.CurrentMap = Spawns[i].Map;

                        Spawns[i].X = newx;
                        Spawns[i].Y = newy;
                        Spawns[i].Map = newmap;


                        //Check for Z-Dimension
                        if (!Spawns[i].ProjectileBase.IgnoreZDimension)
                        {
                            Attribute attribute = Globals.GameMaps[Spawns[i].Map].Attributes[Spawns[i].X, Spawns[i].Y];
                            if (attribute != null && attribute.value == (int) Enums.MapAttributes.ZDimension)
                            {
                                if (attribute.data1 > 0)
                                {
                                    Spawns[i].Z = attribute.data1 - 1;
                                }
                            }
                        }

                        int c = TempEntity.CanMove(Dir);
                        Target = (int)TempEntity.CollisionIndex;

                        if (c == -1) //No collision so increase the counter for the next collision detection.
                        {
                            Spawns[i].TransmittionTimer = Environment.TickCount + (long)((float)Globals.GameProjectiles[ProjectileNum].Speed / (float)Globals.GameProjectiles[ProjectileNum].Range);
                            if (Spawns[i].Distance >= Globals.GameProjectiles[ProjectileNum].Range)
                            {
                                killSpawn = true;
                            }
                        }
                        else if (c < -1)
                        {
                            if (c == -2)
                            {
                                if (!Spawns[i].ProjectileBase.IgnoreMapBlocks)
                                {
                                    killSpawn = true;
                                }
                            }
                            else if (c == -3)
                            {
                                if (!Spawns[i].ProjectileBase.IgnoreZDimension)
                                {
                                    killSpawn = true;
                                }
                            }
                            else if (c == -5)
                            {
                                killSpawn = true;
                            }
                        }
                        else
                        {
                            if (c == (int)Enums.EntityTypes.Player) //Player
                            {
                                if (Owner.MyIndex != Target)
                                {
                                    Owner.TryAttack(Target, true, IsSpell);
                                    killSpawn = true; //Remove from the list being processed
                                }
                            }
                            else if (c == (int)Enums.EntityTypes.Resource)
                            {
                                if ((((Resource)Globals.Entities[Target]).IsDead && !Spawns[i].ProjectileBase.IgnoreExhaustedResources) || (!((Resource)Globals.Entities[Target]).IsDead && !Spawns[i].ProjectileBase.IgnoreActiveResources))
                                {
                                    if (Owner.GetType() == typeof(Player))
                                    {

                                        Owner.TryAttack(Target, true, IsSpell);
                                        killSpawn = true; //Remove from the list being processed
                                    }
                                }
                            }
                            else //Any other target
                            {
                                if (Owner.GetType() == typeof(Player))
                                {
                                    
                                    Owner.TryAttack(Target, true, IsSpell);
                                    killSpawn = true; //Remove from the list being processed
                                }
                            }
                        }
                        if (killSpawn)
                        {
                            Spawns[i].Dispose(i);
                            Spawns[i] = null;
                            _spawnCount--;
                        }
                    }
                }
            }
            else
            {
                Globals.GameMaps[CurrentMap].RemoveProjectile(this);
                PacketSender.SendEntityLeave(MyIndex, (int)Enums.EntityTypes.Projectile, CurrentMap);
                Globals.Entities[MyIndex] = null;
            }
        }

        public byte[] Data()
        {
            var bf = new ByteBuffer();
            bf.WriteBytes(base.Data());
            bf.WriteInteger(ProjectileNum);
            bf.WriteInteger(Dir);
            bf.WriteInteger(Target);
            return bf.ToArray();
        }
    }

    public class ProjectileSpawns
    {
        public int X;
        public int Y;
        public int Z;
        public int Map;
        public int Dir;
        public int Distance = 0;
        public ProjectileStruct ProjectileBase;
        public long TransmittionTimer = Environment.TickCount;
        private int _baseEntityIndex;

        public ProjectileSpawns(int dir, int x, int y, int z, int map, ProjectileStruct projectileBase, int parentIndex)
        {
            Map = map;
            X = x;
            Y = y;
            Z = z;
            Dir = dir;
            ProjectileBase = projectileBase;
            _baseEntityIndex = parentIndex;
        }

        public void Dispose(int spawnIndex)
        {
            PacketSender.SendRemoveProjectileSpawn(Map,_baseEntityIndex, spawnIndex);
        }
    }
}