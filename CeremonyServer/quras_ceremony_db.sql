-- phpMyAdmin SQL Dump
-- version 4.8.0.1
-- https://www.phpmyadmin.net/
--
-- Host: 127.0.0.1
-- Generation Time: Nov 19, 2020 at 03:24 PM
-- Server version: 10.1.32-MariaDB
-- PHP Version: 7.1.17

SET SQL_MODE = "NO_AUTO_VALUE_ON_ZERO";
SET AUTOCOMMIT = 0;
START TRANSACTION;
SET time_zone = "+00:00";


/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!40101 SET NAMES utf8mb4 */;

--
-- Database: `quras_ceremony_db`
--

-- --------------------------------------------------------

--
-- Table structure for table `tbl_status`
--

CREATE TABLE `tbl_status` (
  `id` int(11) NOT NULL,
  `started_at` datetime NOT NULL,
  `completed_at` datetime NOT NULL,
  `status` int(11) NOT NULL DEFAULT '0' COMMENT '0: waiting, 1: started. 2: completed',
  `privkey` varchar(256) COLLATE utf8_unicode_ci NOT NULL,
  `pubkey` varchar(256) COLLATE utf8_unicode_ci NOT NULL,
  `seed` varchar(256) COLLATE utf8_unicode_ci NOT NULL DEFAULT '00',
  `prev_participant_time` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `segment_num` int(11) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;

-- --------------------------------------------------------

--
-- Table structure for table `tbl_user`
--

CREATE TABLE `tbl_user` (
  `id` int(11) NOT NULL COMMENT 'id',
  `name` varchar(64) COLLATE utf8_unicode_ci NOT NULL COMMENT 'name',
  `email` varchar(64) COLLATE utf8_unicode_ci NOT NULL COMMENT 'email address',
  `password` binary(32) NOT NULL,
  `address` varchar(64) COLLATE utf8_unicode_ci NOT NULL COMMENT 'address in the system',
  `pubkey` varchar(68) COLLATE utf8_unicode_ci NOT NULL COMMENT 'pubkey in the system',
  `country` varchar(64) COLLATE utf8_unicode_ci NOT NULL COMMENT 'country',
  `order` int(11) NOT NULL COMMENT 'order',
  `status` int(11) NOT NULL DEFAULT '0' COMMENT '0: waiting, 1: operating, 2: successed, 3: timeout, 4: blocked',
  `spent_time` int(11) NOT NULL COMMENT 'spent time',
  `created_at` timestamp NULL DEFAULT CURRENT_TIMESTAMP COMMENT 'created at',
  `updated_at` timestamp NOT NULL DEFAULT '0000-00-00 00:00:00' ON UPDATE CURRENT_TIMESTAMP COMMENT 'updated_at'
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci ROW_FORMAT=DYNAMIC;

--
-- Indexes for dumped tables
--

--
-- Indexes for table `tbl_status`
--
ALTER TABLE `tbl_status`
  ADD PRIMARY KEY (`id`);

--
-- Indexes for table `tbl_user`
--
ALTER TABLE `tbl_user`
  ADD PRIMARY KEY (`id`) USING BTREE,
  ADD UNIQUE KEY `unique_email` (`email`);

--
-- AUTO_INCREMENT for dumped tables
--

--
-- AUTO_INCREMENT for table `tbl_status`
--
ALTER TABLE `tbl_status`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=2;

--
-- AUTO_INCREMENT for table `tbl_user`
--
ALTER TABLE `tbl_user`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT COMMENT 'id', AUTO_INCREMENT=5;
COMMIT;

/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
