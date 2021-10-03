# DRONAI
<img src="https://github.com/osamhack2021/app_web_dronai_62bn/blob/3af0d4a39b5353f4054cd5a518ac02a18db99172/WEB/logo-images/twitter_header_photo_2.png" width = 100% height = 100%>

## 프로잭트 소개
- 드론 전투체계 관리 콘솔
- [Live Demo](https://dronai.linearjun.com)
- [실시간 기획](https://dronai.notion.site/dronai/DRONAI-44534bc31aac4efaa2b24e3480d71581)


## 기능 설명
 - 설명 기입


## 컴퓨터 구성 / 필수 조건 안내 (Prerequisites)
* ECMAScript 6 지원 브라우저 사용
* 권장: Google Chrome 버젼 77 이상


## 기술 스택 (Technique Used)
### Front-end (Dashboard)
 -  Module: react.js, react-redux, react-router
 -  Theme: material-ui, react-berry

### Back-end (Api & Socket)
 - Language: javascript, typescript, scss
 - Module: nodejs, express, socket, bcrypt, passport
 - Database: sqlite3

### Simulation (Client)
 - Language: C#
 - Tools: [Unity3d](https://unity.com)
 - Library: [Priority Queue - MIT](https://github.com/BlueRaja/High-Speed-Priority-Queue-for-C-Sharp)


## 설치 안내 (Installation Process)
-  API 서버 실행
```bash
$ cd .\dronai-api\
$ yarn
$ yarn typeorm migration:run
$ yarn dev
```
-  DASHBOARD FRONTEND 실행
```bash
$ cd .\dronai-dashboard\
$ yarn
$ yarn start
```

## 팀 정보 (Team Information)
- KIM JUN YOUNG (admin@linearjun.com), Github Id: linearjun
- KANG GEON GU (kdr06006@naver.com), Github Id: kanggeongu
- KO GEON WOO (coreax7@gmail.com), Github Id: gwsl 
- HAN CHOONG HYUN (kd4aqqjr@naver.com), Github Id: keyboardsejong


## 저작권 및 사용권 정보 (Copyleft / End User License)
 * [MIT](https://github.com/osam2020-WEB/Sample-ProjectName-TeamName/blob/master/license.md)

This project is licensed under the terms of the MIT license.
