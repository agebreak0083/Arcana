# Arcana - Unity Tactics Game

## 🎮 프로젝트 소개
턴제 전략 게임 Arcana의 Unity 프로젝트입니다.

## 📋 필수 요구사항

### Unity 버전
- Unity 2021.3 LTS 이상

### Firebase SDK 설치 (필수)
이 프로젝트는 Firebase Realtime Database를 사용합니다. Firebase SDK는 파일 크기가 커서 Git 저장소에 포함되지 않습니다.

#### Firebase SDK 설치 방법

1. **Firebase Unity SDK 다운로드**
   - https://firebase.google.com/download/unity 접속
   - 최신 버전 다운로드 (예: `firebase_unity_sdk_11.x.x.zip`)

2. **필요한 패키지 임포트**
   - Unity 에디터 열기
   - `Assets` → `Import Package` → `Custom Package...`
   - 다운로드한 SDK에서 다음 패키지 임포트:
     - `FirebaseDatabase.unitypackage` (필수)
     - `FirebaseAuth.unitypackage` (선택)

3. **Firebase 설정 파일**
   - Firebase Console에서 `google-services.json` 다운로드
   - `Assets/` 폴더에 배치

4. **의존성 해결**
   - Unity 에디터에서 자동으로 의존성 다운로드
   - `Assets` → `External Dependency Manager` → `Android Resolver` → `Force Resolve`

자세한 설정 방법은 `Firebase_Setup_Guide.md` 파일을 참고하세요.

## 🚀 시작하기

### 1. 프로젝트 클론
```bash
git clone https://github.com/agebreak0083/Arcana.git
cd Arcana
```

### 2. Unity에서 프로젝트 열기
- Unity Hub에서 "Add" 클릭
- 클론한 프로젝트 폴더 선택

### 3. Firebase SDK 설치
위의 "Firebase SDK 설치" 섹션 참고

### 4. 씬 설정
- `TacticsScene` 또는 `BattleScene` 열기
- FirebaseManager 오브젝트가 씬에 있는지 확인

### 5. 실행
- Play 버튼 클릭
- Console에서 "Firebase 초기화 성공!" 로그 확인

## 📁 프로젝트 구조

```
Assets/
├── Scripts/
│   ├── FirebaseManager.cs          # Firebase 연동
│   ├── UserDataManager.cs          # 유저 데이터 관리
│   ├── Tactics/                    # 전술 시스템
│   │   ├── TacticsDataManager.cs
│   │   ├── UI/
│   │   └── Data/
│   └── ...
├── Resources/
│   ├── Table/                      # 게임 데이터 테이블
│   │   ├── CharacterList.json
│   │   ├── ClassList.json
│   │   └── SkillList.json
│   ├── tactics.json                # 전술 데이터
│   └── CharacterPool.json          # 캐릭터 풀
└── Scenes/
    ├── TacticsScene.unity          # 전술 편성 씬
    └── BattleScene.unity           # 전투 씬
```

## 🔧 주요 기능

### Tactics System
- 캐릭터 편성 및 전술 설정
- 스킬 선택 및 조건 설정
- Firebase에 자동 저장

### Firebase Integration
- Realtime Database를 통한 전술 데이터 저장
- 유저별 데이터 관리
- 키 형식: `{username}_{timestamp}`

### User Data Management
- 플레이어 이름 및 티켓 관리
- 게임 설정 저장
- 로컬 저장 + Firebase 동기화

## 📖 문서

- `Firebase_Setup_Guide.md` - Firebase 상세 설정 가이드
- `Firebase_QuickStart.md` - 5분 빠른 시작 가이드

## 🛠️ 개발 환경

- **Unity**: 2021.3 LTS 이상
- **Firebase**: Realtime Database
- **언어**: C#
- **플랫폼**: Windows, Android, iOS

## ⚠️ 주의사항

### Firebase SDK
- Firebase SDK 파일들은 `.gitignore`에 포함되어 있습니다
- 각 개발자가 직접 다운로드하여 설치해야 합니다
- `google-services.json`은 Git에 포함되어 있습니다 (프로젝트 설정용)

### 빌드 설정
- Android: Minimum API Level 21 이상
- iOS: iOS 11.0 이상

## 🤝 기여하기

1. Fork the Project
2. Create your Feature Branch (`git checkout -b feature/AmazingFeature`)
3. Commit your Changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the Branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## 📝 라이선스

이 프로젝트는 개인 프로젝트입니다.

## 📧 연락처

- GitHub: [@agebreak0083](https://github.com/agebreak0083)
- Project Link: [https://github.com/agebreak0083/Arcana](https://github.com/agebreak0083/Arcana)

---

**Last Updated**: 2025-12-06
