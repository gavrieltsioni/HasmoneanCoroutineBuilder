# What is CoroutineBuilder

CoroutineBuilder helps you easily write and compose Unity Coroutines using a fluent builder.

# Usage examples

```csharp
using Hasmonean.Coroutines;

// Create game logic
StartCoroutine(CoroutineBuilder.New()
    .ThenDo(() => swooshSoundEffect.Play())
    .ThenWait(SwingSword(target.position))
    .ThenDo(() => target.TakeDamage())
    .Build());

// Build event-driven systems
StartCoroutine(CoroutineBuilder.New()
    .ThenWaitForEvent((observed) => {
        bossDefeated += observed; // 'bossDefeated' is a C# event
        return () => bossDefeated -= observed; // Clean up
    })
    .ThenWaitForAll(this, FadeIn(youWinMessage), FadeIn(highScoreScreen))
    .ThenNotify(playerWonTheGameEvent)
    .Build());

// Build your tutorial
StartCoroutine(CoroutineBuilder.New()
    .ThenDo(() => SetTutorialMessage("This game is a tower defense game."))
    .ThenWait(FadeIn(tutorialMessage))
    .ThenWaitUntil(() => userPressedContinue)
    .ThenWait(FadeOut(tutorialMessage))
    .ThenDo(() => SetTutorialMessage("You can buy towers in the store."))
    .ThenWait(FadeIn(tutorialMessage))
    .ThenWaitUntil(() => userPurchasedSomething)
    .ThenWait(FadeOut(tutorialMessage))
    .ThenNotify(tutorialCompletedEvent)
    .Build());

// Leverage convenient factory methods
StartCoroutine(CoroutineBuilder.Every(15.0f, () => money += 10));
StartCoroutine(CoroutineBuilder.Repeat(3, () => CoroutineBuilder.New()
    .ThenWait(SendWaveOfEnemies())
    .Build()));
StartCoroutine(CoroutineBuilder.Timed(15.0f, () => TrackPlayer(player.position)));
```

# Why use CoroutineBuilder?

Unity's built-in Coroutines are built on top of C#'s iterator pattern: methods return IEnumerator, and the `yield` keyword is used to suspend execution until the next frame.

```csharp
IEnumerator FadeIn(CanvasGroup canvasGroup)
{
    for (float ratio = 0.0f; ratio <= 1.0f; ratio += 0.1f)
    {
        canvasGroup.alpha = Mathf.SmoothStep(0.0f, 1.0f, ratio);
        yield return null;
    }
}
```

## What problems CoroutineBuilder solves

### Define Coroutines inline

Vanilla Coroutines cannot be defined inline, leading to many disposable one-off methods to define coroutine logic:

```csharp
// Vanilla coroutines
StartCoroutine(DisplayAlertInTenSeconds());

IEnumerator DisplayAlertInTenSeconds()
{
    yield return new WaitForSeconds(10.0f);
    yield return DisplayAlert();
}
```

This also means that Coroutines cannot be easily passed as arguments to other methods, nor can they be defined as lambdas:

```csharp
// Vanilla coroutines
IEnumerator UsePooledGameObject(Func<GameObject, IEnumerator> coroutineFactory);

StartCoroutine(UsePooledGameObject((gameObject) => {
    yield return null; // Not allowed!
}));

StartCoroutine(UsePooledGameObject((gameObject) => MoveObject(gameObject)));

IEnumerator MoveObject(GameObject gameObject)
{
    // ...
}
```

With CoroutineBuilder, both of these cases can be simplified by defining the coroutines inline, without the need for a one-off method.

```csharp
// With CoroutineBuilder
StartCoroutine(CoroutineBuilder.New()
    .ThenWaitForSeconds(10.0f)
    .ThenWait(DisplayAlert())
    .Build());

StartCoroutine(UsePooledGameObject((gameObject) => CoroutineBuilder.New()
    .ThenWait( /* ... */ )
    .Build()));
```

### Compose Coroutines easily

Vanilla Coroutines have no built-in tools for composition. For example, to run several coroutines in sequence, you need to define another Coroutine (as a one-off method) which `yield return`s them:

```csharp
// Vanilla coroutines
StartCoroutine(MoveThenExplodeMissile());

IEnumerator MoveThenExplodeMissile()
{
    yield return MoveMissile();
    yield return ExplosionEffect();
}

IEnumerator MoveMissile() { /* ... */ }

IEnumerator ExplosionEffect() { /* ... */ }
```

With CoroutineBuilder, this can be accomplished without being forced to create a new method:

```csharp
// With CoroutineBuilder
StartCoroutine(CoroutineBuilder.New()
    .ThenWait(MoveMissile())
    .ThenWait(ExplosionEffect())
    .Build());

IEnumerator MoveMissile() { /* ... */ }

IEnumerator ExplosionEffect() { /* ... */ }
```

CoroutineBuilder also has these compositions:

```csharp
// With CoroutineBuilder
StartCoroutine(CoroutineBuilder.New()
    .ThenStart(this, ScreenShake(10.0f)) // Starts a coroutine in parallel, without waiting for it to finish. 'this' is a MonoBehaviour
    .ThenWaitForAll(this, FadeIn(healthBar), DisplayMessage("Boss fight")) //Starts all coroutines, and waits for them all to finish before proceeding. 'this' is a MonoBehaviour
    .ThenNotify(bossFightStarted)
    .Build());
```