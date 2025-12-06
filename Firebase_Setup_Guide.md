# Firebase Realtime Database 설정 가이드

## 📋 목차
1. [Firebase 프로젝트 설정](#1-firebase-프로젝트-설정)
2. [Unity Firebase SDK 설치](#2-unity-firebase-sdk-설치)
3. [Unity 프로젝트 설정](#3-unity-프로젝트-설정)
4. [사용 방법](#4-사용-방법)
5. [데이터 구조](#5-데이터-구조)
6. [문제 해결](#6-문제-해결)

---

## 1. Firebase 프로젝트 설정

### 1.1 Firebase Console에서 프로젝트 생성

1. **Firebase Console 접속**
   - https://console.firebase.google.com/ 접속
   - Google 계정으로 로그인

2. **새 프로젝트 생성**
   - "프로젝트 추가" 버튼 클릭
   - 프로젝트 이름 입력: `Arcana-Game` (원하는 이름)
   - Google Analytics 설정 (선택사항, 나중에 추가 가능)
   - "프로젝트 만들기" 클릭

### 1.2 Realtime Database 활성화

1. **데이터베이스 생성**
   - 좌측 메뉴: "빌드" → "Realtime Database" 선택
   - "데이터베이스 만들기" 클릭
   - 위치 선택: `asia-southeast1` (서울과 가까운 지역)
   - 보안 규칙 선택:
     - **테스트 모드**: 개발 중 (30일 후 만료)
     - **잠금 모드**: 나중에 규칙 설정

2. **보안 규칙 설정** (중요!)
   - "규칙" 탭 클릭
   - 아래 규칙 복사 후 붙여넣기:

```json
{
  "rules": {
    "tactics": {
      "$key": {
        ".read": true,
        ".write": true
      }
    }
  }
}
```

   - "게시" 버튼 클릭

**⚠️ 프로덕션 환경용 보안 규칙** (인증 사용 시):
```json
{
  "rules": {
    "tactics": {
      "$key": {
        ".read": "auth != null",
        ".write": "auth != null"
      }
    }
  }
}
```

### 1.3 Unity 앱 등록

1. **Unity 앱 추가**
   - Firebase Console 프로젝트 개요 페이지
   - Unity 아이콘 클릭 (또는 "앱 추가" → Unity 선택)

2. **패키지 이름 입력**
   - Unity 에디터에서: `Edit` → `Project Settings` → `Player`
   - "Other Settings" → "Package Name" 확인
   - 예: `com.yourcompany.arcana`
   - Firebase Console에 동일한 패키지 이름 입력

3. **구성 파일 다운로드**
   - `google-services.json` 파일 다운로드
   - **중요**: 이 파일을 `Assets/` 폴더에 배치
   - 경로: `c:\Project\Arcana\Assets\google-services.json`

---

## 2. Unity Firebase SDK 설치

### 2.1 Firebase Unity SDK 다운로드

1. **SDK 다운로드**
   - https://firebase.google.com/download/unity 접속
   - 최신 버전 다운로드 (예: `firebase_unity_sdk_11.x.x.zip`)
   - ZIP 파일 압축 해제

2. **필요한 패키지**
   - `FirebaseDatabase.unitypackage` (필수)
   - `FirebaseAuth.unitypackage` (인증 사용 시)

### 2.2 Unity에 패키지 임포트

1. **Unity 에디터에서 임포트**
   - `Assets` → `Import Package` → `Custom Package...`
   - 압축 해제한 폴더에서 `FirebaseDatabase.unitypackage` 선택
   - "Import" 클릭 (모든 파일 선택)

2. **의존성 해결**
   - Firebase SDK가 자동으로 필요한 의존성 다운로드
   - Unity 에디터 하단에 진행 상황 표시
   - 완료될 때까지 대기 (몇 분 소요 가능)

---

## 3. Unity 프로젝트 설정

### 3.1 FirebaseManager 오브젝트 생성

1. **씬에 빈 오브젝트 추가**
   - Hierarchy 우클릭 → "Create Empty"
   - 이름: `FirebaseManager`

2. **FirebaseManager 스크립트 추가**
   - `FirebaseManager` 오브젝트 선택
   - Inspector에서 "Add Component"
   - `FirebaseManager` 스크립트 선택

3. **DontDestroyOnLoad 확인**
   - FirebaseManager는 자동으로 씬 전환 시에도 유지됨

### 3.2 빌드 설정 확인

1. **Android 빌드 설정** (Android용)
   - `File` → `Build Settings`
   - Platform: Android 선택
   - "Switch Platform" 클릭
   - `Player Settings` → `Other Settings`
   - "Minimum API Level": Android 5.0 이상

2. **iOS 빌드 설정** (iOS용)
   - Platform: iOS 선택
   - "Switch Platform" 클릭
   - Xcode 프로젝트 생성 후 추가 설정 필요

---

## 4. 사용 방법

### 4.1 자동 저장 (현재 구현됨)

tactics.json이 저장될 때 자동으로 Firebase에도 저장됩니다:

```csharp
// TacticsDataManager.cs의 SaveFormationToTacticsFile 메서드에서 자동 호출
_dataManager.SaveFormationToTacticsFile(_unitSlots, _codingData);
```

### 4.2 수동으로 Firebase에 저장

```csharp
// tactics.json 내용을 문자열로 읽기
string tacticsJson = System.IO.File.ReadAllText("path/to/tactics.json");

// Firebase에 저장
FirebaseManager.Instance.SaveTacticsToFirebase(tacticsJson, (success, key) =>
{
    if (success)
    {
        Debug.Log($"저장 완료! 키: {key}");
    }
    else
    {
        Debug.LogError($"저장 실패: {key}");
    }
});
```

### 4.3 Firebase에서 데이터 로드

```csharp
// 특정 키로 데이터 로드
string key = "agebreak-wo2_2512061905";
FirebaseManager.Instance.LoadTacticsFromFirebase(key, (success, tacticsJson) =>
{
    if (success)
    {
        Debug.Log("로드 완료!");
        // tacticsJson을 파일로 저장하거나 직접 사용
    }
});
```

### 4.4 유저의 모든 Tactics 키 목록 가져오기

```csharp
string username = "agebreak-wo2";
FirebaseManager.Instance.GetUserTacticsKeys(username, (success, keys) =>
{
    if (success)
    {
        foreach (string key in keys)
        {
            Debug.Log($"발견된 키: {key}");
        }
    }
});
```

---

## 5. 데이터 구조

### 5.1 Firebase Database 구조

```
firebase-database/
└── tactics/
    ├── agebreak-wo2_2512061905/
    │   ├── username: "agebreak-wo2"
    │   ├── timestamp: "2025-12-06 19:05:00"
    │   └── tacticsJson: "{...tactics.json 내용...}"
    ├── agebreak-wo2_2512061910/
    │   └── ...
    └── player2_2512061920/
        └── ...
```

### 5.2 저장되는 데이터 예시

**Key**: `agebreak-wo2_2512061905`

**Value**:
```json
{
  "username": "agebreak-wo2",
  "timestamp": "2025-12-06 19:05:00",
  "tacticsJson": "{\"positions\":[...]}"
}
```

### 5.3 키 생성 규칙

- 형식: `{username}_{timestamp}`
- Username: 소문자, 특수문자 제거, 공백은 하이픈(-)으로 변환
- Timestamp: `yyMMddHHmm` 형식 (예: 2512061905 = 2025년 12월 6일 19시 05분)

---

## 6. 문제 해결

### 6.1 "Firebase가 초기화되지 않았습니다" 오류

**원인**: Firebase SDK가 제대로 초기화되지 않음

**해결 방법**:
1. `google-services.json` 파일이 `Assets/` 폴더에 있는지 확인
2. Unity 에디터 재시작
3. Firebase SDK가 완전히 임포트되었는지 확인
4. Console 창에서 Firebase 초기화 로그 확인

### 6.2 "DependencyStatus.UnavailableOther" 오류

**원인**: Firebase 의존성 문제

**해결 방법**:
1. `Assets` → `External Dependency Manager` → `Android Resolver` → `Force Resolve`
2. Unity 에디터 재시작
3. Firebase SDK 재설치

### 6.3 Android 빌드 시 오류

**원인**: Gradle 설정 문제

**해결 방법**:
1. `File` → `Build Settings` → `Player Settings`
2. `Publishing Settings` → "Custom Main Gradle Template" 활성화
3. Minimum API Level을 21 이상으로 설정

### 6.4 데이터가 저장되지 않음

**원인**: 보안 규칙 또는 네트워크 문제

**해결 방법**:
1. Firebase Console에서 보안 규칙 확인
2. 인터넷 연결 확인
3. Firebase Console의 "Realtime Database" → "데이터" 탭에서 수동으로 확인

### 6.5 "UserDataManager not initialized" 오류

**원인**: UserDataManager가 씬에 없음

**해결 방법**:
1. 씬에 UserDataManager 오브젝트 추가
2. UserDataManager 스크립트 연결
3. FirebaseManager보다 먼저 초기화되도록 설정

---

## 7. 테스트 방법

### 7.1 Unity 에디터에서 테스트

1. **Play 모드 실행**
2. **Tactics 데이터 저장**
   - Tactics 화면에서 편성 변경
   - "Run Battle" 버튼 클릭 (자동 저장)
3. **Firebase Console 확인**
   - Firebase Console → Realtime Database → 데이터 탭
   - `tactics/` 노드에 데이터가 추가되었는지 확인

### 7.2 로그 확인

Unity Console에서 다음 로그 확인:
```
Firebase 초기화 성공!
Formation saved to ...
Firebase에 Tactics 저장 완료: agebreak-wo2_2512061905
```

---

## 8. 추가 기능 구현 아이디어

### 8.1 리더보드 기능
- 모든 유저의 최신 Tactics 데이터 표시
- 인기 있는 전략 순위

### 8.2 Tactics 공유 기능
- 특정 키를 다른 유저와 공유
- QR 코드로 Tactics 공유

### 8.3 버전 관리
- 같은 유저의 여러 버전 Tactics 저장
- 이전 버전으로 롤백 기능

### 8.4 클라우드 동기화
- 여러 기기에서 동일한 Tactics 사용
- 자동 백업 및 복원

---

## 9. 보안 권장사항

### 9.1 프로덕션 환경 설정

1. **Firebase Authentication 사용**
   - 익명 인증 또는 이메일 인증 구현
   - 보안 규칙에 인증 조건 추가

2. **보안 규칙 강화**
```json
{
  "rules": {
    "tactics": {
      "$key": {
        ".read": "auth != null",
        ".write": "auth != null && data.child('username').val() == auth.uid"
      }
    }
  }
}
```

3. **데이터 검증**
   - 클라이언트에서 보내는 데이터 검증
   - 서버 측 검증 규칙 추가

---

## 10. 참고 자료

- [Firebase Unity 공식 문서](https://firebase.google.com/docs/unity/setup)
- [Realtime Database 가이드](https://firebase.google.com/docs/database/unity/start)
- [보안 규칙 문서](https://firebase.google.com/docs/database/security)
- [Firebase Console](https://console.firebase.google.com/)

---

**작성일**: 2025-12-06  
**버전**: 1.0  
**작성자**: Antigravity AI Assistant
