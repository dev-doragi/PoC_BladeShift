## **0. 핵심 지침 (Core Principles)**
* **MainScene 보호**: 핵심 시스템 구현 단계이므로 `MainScene`은 수정하지 않고 비워둡니다.
* **개인 작업 환경**: 각 작업자는 자신의 이름으로 된 전용 씬을 생성하여 테스트를 진행합니다. (예: `JaeinScene`)
* **씬 관리**: 모든 씬 파일(.unity)은 `00.Scenes` 폴더 내에 저장합니다.
* **버전 정보**: 프로젝트는 **Unity 6000.3.9f1** 버전을 사용합니다.

---

## **1. 폴더 구조 (Folder Structure)**
프로젝트 뷰의 가독성과 정렬을 위해 아래의 인덱싱 구조를 엄격히 따릅니다.

* **00.Scenes**: 씬 파일 (`.unity`)
* **01.Scripts**: C# 스크립트
* **02.Prefabs**: 재사용 가능한 프리팹
* **03.Art**: 3D 모델, 2D 소스, 메테리얼 등 아트 리소스
* **04.UI**: UI 프리팹, 아틀라스 및 스프라이트
* **05.Audio**: BGM, SFX 오디오 파일
* **06.VFX**: 파티클 시스템, 셰이더 및 이펙트
* **07.Data**: ScriptableObject, JSON, CSV 등 데이터 에셋
* **09.Fonts**: 폰트 에셋
* **98.Debugger**: 디버그 전용 툴, 스크립트 및 테스트 프리팹
* **99.Test**: 개인별 샌드박스 씬 및 임시 테스트 스크립트

---

## **2. Git 브랜치 전략 (GitFlow)**
* **main**: 최종 배포 및 빌드용 브랜치.
* **dev**: 개발 통합 브랜치. 모든 기능 구현 결과가 모이는 중심.
* **feature/**: 단위 기능 구현 브랜치. (예: `feature/player-movement`)

---

## **3. 코드 컨벤션 (C# Naming Convention)**
* **PascalCase**: 클래스(Class), 메서드(Method), 프로퍼티(Property)
* **_camelCase**: `private` 및 `protected` 필드. 접두어 언더바(`_`) 사용 필수.
* **camelCase**: 지역 변수(Local Variable), 파라미터(Parameter)

---

## **4. 프로그래밍 규칙 (Programming Rules)**

### **필수 패키지 및 에셋 활용 (Required Packages)**
* **입력 시스템 (Input System)**: 레거시 Input Manager의 사용을 엄격히 금지하며, 오직 **New Input System**만을 사용합니다.
* **UI 텍스트 (UI Text)**: 레거시 Text 컴포넌트 사용을 금지하고, 반드시 **TextMeshPro (TMP)**를 사용합니다.
* **트위닝 (Tweening)**: 코드 기반의 애니메이션 및 트위닝 연출은 **DOTween**을 활용합니다.

### **싱글톤 및 매니저 관리 (Singleton & Bootstrapper)**
* **중앙 통제 초기화**: 모든 싱글톤 매니저의 인스턴스 생성과 초기화 순서는 `Bootstrapper` 클래스가 통제합니다. 개별 클래스에서의 자의적인 초기화를 금지합니다.
* **명시적 초기화**: 매니저의 실제 데이터 로드 및 셋업은 `Awake`가 아닌 `BootstrapIfNeeded()` 내의 `OnBootstrap()`에서 수행합니다.
* **인스턴스 접근**: 타 매니저의 기능이 불가피하게 필요할 경우 `ManagerName.Instance`를 통해 접근하되, 의존성을 최소화하기 위해 가급적 이벤트 버스를 활용합니다.

### **이벤트 기반 통신 (Event Bus)**
* **의존성 분리**: 시스템 간, 매니저 간의 직접적인 참조(Coupling)를 지양하고 `EventBus`를 통한 발행(Publish) 및 구독(Subscribe) 패턴을 사용합니다.
* **구독 생명주기**: 이벤트 메모리 누수를 방지하기 위해 이벤트 구독(`Subscribe`)은 `OnEnable` 또는 초기화 시점에, 해제(`Unsubscribe`)는 반드시 `OnDisable` 또는 `OnDestroy`에서 수행합니다.

### **엄격한 예외 처리 (Strict Null Check)**
* **에러 로그 강제**: 참조 확인 시 단순 `return` 처리를 금지합니다. 반드시 `Debug.LogError()`를 호출하여 콘솔에 에러를 명시하고 로직을 즉시 중단합니다.

---

## **5. 작업 흐름 요약**
1. `dev`에서 `feature/기능-이름` 브랜치 생성.
2. 자신의 이름으로 된 테스트 씬에서 기능을 구현.
3. 머지 전 `dev`를 자신의 브랜치로 가져와(Pull/Merge) 충돌 해결.
4. 컴파일 에러 및 로그 상 결함이 없는 상태로 `dev`에 병합.
