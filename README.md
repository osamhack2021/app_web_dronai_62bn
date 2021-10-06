# DRONAI

<!-- PROJECT LOGO -->
<br />
<div align="center">
  <a href="https://github.com/osamhack2021/app_web_dronai_62bn">
    <img src="https://github.com/osamhack2021/app_web_dronai_62bn/blob/master/WEB/logo-images/logo_only.png" alt="Logo" width="256px" height="256px">
  </a>

  <h3 align="center">DRONAI</h3>

  <p align="center">
    드론 전투체계 시스템 관리 콘솔
    <br />
    <br />
    <a href="https://dronai.linearjun.com">Web Demo</a>
    ·
    <a href="https://dronai.notion.site/dronai/DRONAI-44534bc31aac4efaa2b24e3480d71581">Notion</a>
    ·
    <a href="https://dronai.gitbook.io/dronai/">Documents</a>
    <br />
    <br />
    <a href="https://github.com/osamhack2021/app_web_dronai_62bn/graphs/contributors">
      <img src="https://img.shields.io/github/contributors/osamhack2021/app_web_dronai_62bn.svg?style=for-the-badge"/>
    </a>
    <a href="https://github.com/osamhack2021/app_web_dronai_62bn/network/members">
      <img src="https://img.shields.io/github/forks/osamhack2021/app_web_dronai_62bn.svg?style=for-the-badge"/>
    </a>
    <a href="https://github.com/osamhack2021/app_web_dronai_62bn/stargazers">
      <img src="https://img.shields.io/github/stars/osamhack2021/app_web_dronai_62bn.svg?style=for-the-badge"/>
    </a>
    <a href="https://github.com/osamhack2021/app_web_dronai_62bn/issues">
      <img src="https://img.shields.io/github/issues/osamhack2021/app_web_dronai_62bn.svg?style=for-the-badge"/>
    </a>
    <a href="https://github.com/osamhack2021/app_web_dronai_62bn/blob/master/license.md">
      <img src="https://img.shields.io/github/license/osamhack2021/app_web_dronai_62bn.svg?style=for-the-badge"/>
    </a>
  </p>
</div>

## :books: 목차
<details open="open">
  <ol>
    <li><a href="#about"> 프로젝트 소개 (About)</a></li>
    <li><a href="#features"> 기능 설명 (Features)</a></li>
    <li><a href="#prerequisites"> 컴퓨터 구성 / 필수 조건 안내 (Prequisites)</a></li>
    <li><a href="#techniques"> 기술 스택 (Techniques)</a></li>
      <ul>
        <li><a href="#front_end"> Front-end (Dashboard)</a></li>
        <li><a href="#back_end"> Back-end (Api & Socket)</a></li>
        <li><a href="#client"> Simulation (Client)</a></li>
      </ul>
    <li><a href="#installation"> 설치 안내 (Installation Process)</a></li>
    <li><a href="#team"> 팀 정보 (Team Information)</a></li>
    <li><a href="#license"> 저작권 및 사용권 정보 (Copyleft / End User License)</a></li>
  </ol>
</details>

<h2 id="about"> :grey_question: 프로젝트 소개</h2>

> 드론 전투체계를 통합적으로 관리하는 플랫폼이다. 

<img src="https://media.wired.com/photos/59327007a312645844994da4/master/w_1600,c_limit/shadows2.gif" height="100%" width="100%"></img>


<h2 id="features"> :mag: 기능 설명</h2>
<blockquote>DRONAI에서 제공하는 기능에 대해 기술하는 영역</blockquote>

<details open="open">
  <ul>
    <li>클라이언트</li>
      <ul>
        <li><a href="#editor">에디터<a/></li>
        <li><a href="#runtime">런타임<a/></li>
        <li><a href="#drone_auto_locate">드론 자동배치 기능 (EDITOR)</a></li>
        <li>etc...</li>
      </ul>
    <li>웹 대시보드</li>
  </ul>
</details>

<hr>

<h3 id="editor">에디터 / EDITOR</h3>
<blockquote>에디터에서만 동작하는 기능</blockquote>
<img src="https://user-images.githubusercontent.com/36218321/136125123-456be0a1-2ec2-4318-bdbd-280101349366.PNG"></img>
<p>DRONAI 프로젝트는 컴파일 된 프로그램뿐만이 아니라 개발단계에서 필요한 에디터 화면에서도 여러가지 기능을 제공한다.</p>
<p>앞으로 에디터에서만 동작하는 기능들에 대해서는 <b>EDITOR</b>라는 약어를 붙히겠다.</p>

<hr>

<h3 id="runtime">런타임 / RUNTIME</h3>
<blockquote>런타임에서 동작하는 기능</blockquote>
<img src="https://user-images.githubusercontent.com/36218321/136126788-88a8390a-6601-414f-b694-a977e0409abf.gif"></img>
<p>컴파일 및 빌드 된 DRONAI 시뮬레이션이 실질적으로 사용자에게 제공하는 기능들이다</p>
<p>앞으로 런타임에서 동작하는 기능들에 대해서는 <b>RUNTIME</b>이라는 약어를 붙히겠다.</p>

<hr>

<h3 id="drone_auto_locate">드론 자동배치 기능 (EDITOR)</h3>
<blockquote>원하는 사이즈와 높이, 생성 빈도에 맞추어 초기 드론 그룹을 자동으로 생성해 준다</blockquote>
<img src="https://user-images.githubusercontent.com/36218321/136124125-f10d4fd4-4b9d-434a-b224-375c17981f74.gif"></img>

<h2 id="prerequisites"> :computer: 컴퓨터 구성 / 필수 조건 안내 (Prerequisites)</h2>

### Web
| <img src="https://user-images.githubusercontent.com/36218321/136121512-c8d2ba2a-4393-48c3-a796-3fc333ffbd6b.png" alt="Chrome" width="16px" height="16px" /> Chrome | <img src="https://user-images.githubusercontent.com/36218321/136121845-7c424ce5-fdc0-4bf6-988d-e69c633ddb8f.png" alt="IE" width="16px" height="16px" /> Internet Explorer | <img src="https://user-images.githubusercontent.com/36218321/136121593-d6f4a166-e330-46da-910e-f1f751b1f57a.png" alt="Edge" width="16px" height="16px" /> Edge | <img src="https://user-images.githubusercontent.com/1215767/34348394-a981f892-ea4d-11e7-9156-d128d58386b9.png" alt="Safari" width="16px" height="16px" /> Safari | <img src="https://user-images.githubusercontent.com/1215767/34348383-9e7ed492-ea4d-11e7-910c-03b39d52f496.png" alt="Firefox" width="16px" height="16px" /> Firefox |
| :---------: | :---------: | :---------: | :---------: | :---------: |
| Yes | No | Yes | Yes | Yes |

### Client (Recommendation)
| <img src="https://user-images.githubusercontent.com/36218321/136122713-f35caddf-423f-4e12-8ecd-0fd4eecfa3c0.png" alt="Processor" width="16px" height="16px" /> Processor | <img src="https://user-images.githubusercontent.com/36218321/136122476-96039289-656b-42fd-87e4-6fbcb0e54822.png" alt="Memory" width="16px" height="16px" /> Memory | <img src="https://user-images.githubusercontent.com/36218321/136122744-f3433980-53a2-43b0-a21f-8650a5d20563.png" alt="Graphics" width="16px" height="16px" /> Graphics | <img src="https://user-images.githubusercontent.com/36218321/136122837-6b83f566-e906-445d-a8ff-af895cecf406.png" alt="DirectX" width="16px" height="16px" /> DirectX | <img src="https://user-images.githubusercontent.com/36218321/136122898-eb30747d-82ec-464f-8acf-1f3986992e06.png" alt="Storage" width="16px" height="16px" /> Storage |
| :---------: | :---------: | :---------: | :---------: | :---------: |
| AMD Athlon X4 <br> Intel Core i5 4460 | 8 GB RAM | Nvidia GTX 950 <br> AMD R7 370 | Version 11 | 2 GB available space |


<h2 id="techniques"> :electric_plug: 기술 스택 (Technique Used)</h2>
<h3 id="front_end">Front-end (Dashboard)</h2>

 -  Module: react.js, react-redux, react-router
 -  Theme: material-ui, react-berry

<h3 id="back_end">Back-end (Api & Socket)</h2>

 - Language: javascript, typescript, scss
 - Module: nodejs, express, socket, bcrypt, passport
 - Database: sqlite3

<h3 id="client">Simulation (Client)</h2>

 - Language: C#
 - Tools: [Unity3d](https://unity.com)
 - Library: [Priority Queue - MIT](https://github.com/BlueRaja/High-Speed-Priority-Queue-for-C-Sharp)


<h2 id="installation"> :information_source: 설치 안내 (Installation Process)</h2>

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

<h2 id="team"> :people_holding_hands: 팀 정보 (Team Information)</h2>

- KIM JUN YOUNG (admin@linearjun.com), Github Id: linearjun
- KANG GEON GU (kdr06006@naver.com), Github Id: kanggeongu
- KO GEON WOO (coreax7@gmail.com), Github Id: gwsl 
- HAN CHOONG HYUN (kd4aqqjr@naver.com), Github Id: keyboardsejong

<h2 id="license"> :scroll: 저작권 및 사용권 정보 (Copyleft / End User License)</h2>

 * [MIT](https://github.com/osam2020-WEB/Sample-ProjectName-TeamName/blob/master/license.md)

This project is licensed under the terms of the MIT license.
