﻿/*
 * Copyright (c) 2020 Proton Technologies AG
 *
 * This file is part of ProtonVPN.
 *
 * ProtonVPN is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * ProtonVPN is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with ProtonVPN.  If not, see <https://www.gnu.org/licenses/>.
 */

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using ProtonVPN.Common.Vpn;
using ProtonVPN.Config;
using ProtonVPN.Core.Api;
using ProtonVPN.Core.Api.Contracts;
using ProtonVPN.Core.Servers.Models;
using ProtonVPN.Core.User;
using ProtonVPN.Core.Vpn;
using ProtonVPN.P2PDetection.Forwarded;
using UserLocation = ProtonVPN.Core.Api.Contracts.UserLocation;

namespace ProtonVPN.App.Test.P2PDetection.Forwarded
{
    [TestClass]
    [SuppressMessage("ReSharper", "ObjectCreationAsStatement")]
    public class ForwardedTrafficTest
    {
        private IUserLocationService _userLocationService;
        private IVpnConfig _vpnConfig;

        [TestInitialize]
        public void TestInitialize()
        {
            _userLocationService = Substitute.For<IUserLocationService>();
            _vpnConfig = Substitute.For<IVpnConfig>();
        }

        [DataTestMethod]
        [DataRow("62.112.9.168", "127.0.0.1", true)]
        [DataRow("62.112.9.168", "62.112.9.168", false)]
        [DataRow("104.245.144.186", "127.0.0.1", true)]
        [DataRow("127.0.0.1", "", false)]
        [DataRow("", "", false)]
        [DataRow(null, "", false)]
        public async Task TrafficShouldBeForwardedWhenUserLocationServiceReturns(string ip, string currentConnectedIP, bool expected)
        {
            // Arrange
            var response = ApiResponseResult<UserLocation>.Ok(new UserLocation
            {
                Ip = ip
            });

            _vpnConfig.BlackHoleIps.Returns(new List<string> { "62.112.9.168", "104.245.144.186" });
            _userLocationService.LocationAsync().Returns(response);
            var subject = new ForwardedTraffic(_userLocationService, _vpnConfig);
            await subject.OnVpnStateChanged(new VpnStateChangedEventArgs(
                VpnStatus.Connected,
                VpnError.None,
                GetConnectedServer(currentConnectedIP),
                false,
                VpnProtocol.Auto));

            // Act
            var result = await subject.Value();

            // Assert
            result.Forwarded.Should().Be(expected);
        }

        private Server GetConnectedServer(string ip)
        {
            return new Server(
                "id",
                "",
                "",
                "",
                "",
                "",
                0,
                0,
                0,
                0,
                0,
                new Location(),
                new List<PhysicalServer>(), ip);
        }
    }
}
