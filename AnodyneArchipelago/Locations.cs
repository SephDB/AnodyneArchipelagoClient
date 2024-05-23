﻿
using System;
using System.Collections.Generic;

namespace AnodyneArchipelago
{
    internal class Locations
    {
        public static Dictionary<Guid, string> LocationsByGuid = new()
        {
            {new Guid("1525ead8-4d92-a815-873a-4dc04037d973"), "Street - Broom Chest" },
            {new Guid("3307aa58-ccf1-fb0d-1450-5af0a0c458f7"), "Street - Key Chest" },
            {new Guid("1bcddffb-5f98-4787-3a81-d1ce8fdeca29"), "Overworld - Near Gate Chest" },
            {new Guid("31e23e62-a487-1657-212c-30a39eef0727"), "Overworld - Health Cicada" },
            {new Guid("a4ffda64-6373-f561-1484-fd30aed00a87"), "Overworld - After Temple Chest" },
            {new Guid("40de36cf-9238-f8b0-7a57-c6c8ca465cc2"), "Temple of the Seeing One - Entrance Chest" },
            {new Guid("401939a4-41ba-e07e-3ba2-dc22513dcc5c"), "Temple of the Seeing One - Dark Room Chest" },
            {new Guid("621c284f-cbd0-74c3-f51b-2a9fdde8d4d7"), "Temple of the Seeing One - Rock-Surrounded Chest" },
            {new Guid("88d0a7b8-eeab-c45f-324e-f1c7885c41ce"), "Temple of the Seeing One - Shieldy Room Chest" },
            {new Guid("c481ee20-662e-6b02-b010-676ab339fc2d"), "Temple of the Seeing One - Health Cicada" },
            {new Guid("0bce5346-48a2-47f9-89cb-3f59d9d0b7d2"), "Temple of the Seeing One - Boss Chest" },
            // Green Key
            {new Guid("d41f2750-e3c7-bbb4-d650-fafc190ebd32"), "Temple of the Seeing One - After Statue Left Chest" },
            {new Guid("3167924e-5d52-1cd6-d6f4-b8e0e328215b"), "Temple of the Seeing One - After Statue Right Chest" },
            {new Guid("a78bd187-a359-243d-9321-5b034b6b3c3c"), "Young Town - Stab Reward Chest" },
            {new Guid("dd551369-9b21-2658-0ec9-1f7a5009e8d6"), "Young Town - Health Cicada" },
            {new Guid("bb72229f-0ffc-f0ba-2678-6e79f29dbdcc"), "Apartment - 1F Exterior Chest" },
            {new Guid("de415e2a-06ee-83ac-f1a3-5dca1fa44735"), "Apartment - 1F Rat Maze Chest" },
            {new Guid("0ac41f72-ee1d-0d32-8f5d-8f25796b6396"), "Apartment - 1F Ledge Chest" },
            {new Guid("c46dd225-39d7-72ac-c48d-4a7e4e56be81"), "Apartment - 1F Couches Chest" },
            {new Guid("5b55a264-3fcd-cf38-175c-141b2d093029"), "Apartment - 2F Rat Maze Chest" },
            {new Guid("2bbf01c8-8267-7e71-5bd4-325001dbc0ba"), "Apartment - 3F Gauntlet Chest" },
            {new Guid("e024c174-ed8e-ea52-743c-4c631549600a"), "Apartment - Health Cicada" },
            {new Guid("0c163884-ec62-1ff3-40b9-5a0b1865e347"), "Apartment - Boss Chest" },
            // Cardboard Box
            {new Guid("34ece95d-b0f8-5a83-6179-e73d9ecf7c0e"), "Fields - Goldman's Cave Chest" },
            // Shopkeeper
            // Mitra
            {new Guid("1d4e36d9-0057-feae-c69a-1e30a072cd30"), "Fields - Island Chest" },
            {new Guid("1aa4fa12-92b4-1cd2-c1c7-694b226576b0"), "Fields - Gauntlet Chest" },
            // Windmill
            {new Guid("8ae5911f-7aac-dc0a-ed27-8090ce0cc045"), "Windmill - Chest" },
            {new Guid("962a7c3d-0ad7-d501-8750-5734235598ee"), "Deep Forest - Inlet Chest" },
            {new Guid("3418e5ed-84ed-f56b-c39c-ada7e4be80d4"), "Cliffs - Lower Chest" },
            {new Guid("b81260fd-281e-8c9c-4fc7-c0600306f1f7"), "Cliffs - Upper Chest" },
            {new Guid("f5cf525f-0a3a-61ac-7d1f-39d2ef17291a"), "Mountain Cavern - Extend Upgrade Chest" },
            {new Guid("be2fb96b-1d5f-fcd1-3f58-d158db982c21"), "Mountain Cavern - 1F Four Enemies Chest" },
            {new Guid("868736ef-ec8b-74c9-acab-b7bc56a44394"), "Mountain Cavern - 1F Frogs and Rotators Chest" },
            {new Guid("5743a883-d209-2518-70d7-869d14925b77"), "Mountain Cavern - 1F Entrance Chest" },
            {new Guid("caab4970-64bc-e3e7-506b-ba822f0beb81"), "Mountain Cavern - 1F Crowded Ledge Chest" },
            {new Guid("21ee2d01-54fb-f145-9464-4c2cc8725eb3"), "Mountain Cavern - 1F Frogs and Dog Chest" },
            {new Guid("e281b455-4842-b1ce-4130-c09d7d5faf21"), "Mountain Cavern - 2F Roller Chest" },
            {new Guid("e2f6a4d0-1a25-4ae6-132c-b8edc2e0126d"), "Mountain Cavern - Health Cicada" },
            {new Guid("04e39b10-28e6-b032-c0ee-0e187fd3ed7c"), "Mountain Cavern - Boss Chest" },
            // Blue Key
            {new Guid("3a8ad9f5-37f2-fdc7-b721-1466f9493c61"), "Space - Left Chest" },
            {new Guid("6bcdcf54-f0c4-8044-adc1-92ef3559351c"), "Space - Right Chest" },
            {new Guid("6c8870d4-7600-6ffd-b425-2d951e65e160"), "Hotel - 4F Annoyers Chest" },
            {new Guid("0dd083df-d8af-30b8-b874-4a08da7c8386"), "Hotel - 4F Dust Blower Maze Chest" },
            {new Guid("64ee884f-ea96-fb09-8a9e-f75abdb6dc0d"), "Hotel - 3F Gasguy Chest" },
            {new Guid("075e6024-fe2d-9c4a-1d2b-d627655fd31a"), "Hotel - 3F Rotators Chest" },
            {new Guid("31b00e25-f8b2-1424-f1b5-48810b00d3e6"), "Hotel - 2F Dog Chest" },
            {new Guid("1990b3a2-dbf8-85da-c372-adafaa75744c"), "Hotel - 2F Crevice Right Chest" },
            {new Guid("d2392d8d-0633-2640-09fa-4b921720bfc4"), "Hotel - 2F Backrooms Chest" },
            {new Guid("9d6fda36-0cc6-bacc-3844-aefb6c5c6290"), "Hotel - 2F Crevice Left Chest" },
            {new Guid("019cbc29-3614-9302-6848-ddaedc7c49e5"), "Hotel - 1F Burst Flowers Chest" },
            {new Guid("87845bd3-09f7-9f4b-04d7-64ce269e7637"), "Hotel - Health Cicada" },
            {new Guid("bd084553-9eee-a478-3d78-4a630725d30e"), "Hotel - Boss Chest" },
            {new Guid("72d94f08-9dae-685c-d024-9ed620329a87"), "Beach - Dock Chest" },
            {new Guid("7a7dbb2e-99b9-d453-0047-d46ae347c1d6"), "Beach - Health Cicada" },
            {new Guid("21b8a756-9b48-3857-d285-a3e66b31f60a"), "Red Sea - Lonely Chest" },
            {new Guid("5ffe5efb-6e92-da36-302a-7b75d3e72085"), "Red Grotto - Widen Upgrade Chest" },
            {new Guid("ae87f1d5-57e0-1749-7e1e-1d0bcc1bcab4"), "Red Grotto - Middle Cave Left Chest" },
            {new Guid("72bad10e-598f-f238-0103-60e1b36f6240"), "Red Grotto - Middle Cave Right Chest" },
            {new Guid("09241266-9657-6152-bb37-5ff0a7fddcf9"), "Red Grotto - Middle Cave Left Tentacle" },
            {new Guid("95d6cf28-66df-54a5-0aec-6b54f56a2edc"), "Red Grotto - Middle Cave Right Tentacle" },
            {new Guid("ed7d4d0f-c29d-d5aa-289a-b6c6ab8a041e"), "Red Grotto - Middle Cave Middle Chest" },
            {new Guid("4a9dc50d-8739-9ad8-2cb1-82ece29d3b6f"), "Red Grotto - Left Cave Rapids Chest" },
            {new Guid("cda1ff45-0f88-4855-b0ec-a9b42376c33f"), "Red Grotto - Left Cave Sticky Chest" },
            {new Guid("a0b1ccc8-849a-b61d-7742-bfaf11013b2a"), "Red Grotto - Left Cave Tentacle" },
            {new Guid("83286bfb-ffda-237e-ba57-ca2e532e1dc7"), "Red Grotto - Right Cave Four Shooter Chest" },
            {new Guid("a7672339-f3fb-c49e-33ce-42a49d7e4533"), "Red Grotto - Right Cave Slasher Chest" },
            {new Guid("ed3b58c5-9191-013c-6935-777766e39a65"), "Red Grotto - Right Cave Tentacle" },
            {new Guid("0504f29a-042a-bbc7-9fd4-5559e8ec64d0"), "Red Grotto - Top Cave Slasher Chest" },
            {new Guid("082409e1-1d7e-3a22-9059-d5d9d0626fb1"), "Red Grotto - Health Cicada" },
            {new Guid("622180eb-bc49-c327-d598-af9714b5ecb3"), "Red Grotto - Boss Chest" },
            // Red Key
            {new Guid("021a2ac3-4fed-13ed-a0fa-88fb641c50f1"), "Labyrinth - Top Left Chest" },
            {new Guid("047260e5-e357-36bb-5454-a99aa3e03524"), "Labyrinth - Health Cicada" },
            {new Guid("69e8fbd6-2da3-d25e-446f-6a59ac3e9fc2"), "Circus - Arthur Chest" },
            {new Guid("13572273-945d-de9e-7758-1ce5890d7fcd"), "Circus - Clowns Chest" },
            {new Guid("75c2d434-4ae8-bcd0-dbeb-8e6cda67bf45"), "Circus - Rat Maze Chest" },
            {new Guid("6a95eb2f-75fd-8649-5e07-3ed37c69a9fb"), "Circus - Javiera Chest" },
            {new Guid("302bf614-753a-f18b-ab2f-5ba3b3798f00"), "Circus - Burners Chest" },
            {new Guid("a2479a02-9b0d-751f-71a4-db15c4982df5"), "Circus - Lion Chest" },
            {new Guid("f37e7952-28d0-74ec-0d54-aeff7a9a31ae"), "Circus - Double Clowns Chest" },
            {new Guid("5a2cda3e-1e41-628c-60fe-91fcb2a0a479"), "Circus - Health Cicada" },
            {new Guid("63182228-f827-7266-50e2-f3becacbf818"), "Circus - Boss Chest" },
            {new Guid("f5fe33c0-272a-d6d2-367b-c54c5caa4a63"), "Terminal - Broken Bridge Chest" },
            {new Guid("48a44dd6-a564-9ac4-f739-d3daa96f5205"), "GO - Swap Upgrade Chest" },
        };
    }
}
