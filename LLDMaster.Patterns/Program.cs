// ═══════════════════════════════════════════════════════════
// LLD Mastery — Design Patterns Entry Point
// Run any pattern demo by uncommenting its block below.
// ═══════════════════════════════════════════════════════════

using LLDMaster.Patterns.Creational.Singleton;
using LLDMaster.Patterns.Creational.FactoryMethod;
using LLDMaster.Patterns.Creational.AbstractFactory;
using LLDMaster.Patterns.Creational.Builder;
using LLDMaster.Patterns.Creational.Prototype;
using LLDMaster.Patterns.Structural.Adapter;
using LLDMaster.Patterns.Structural.Bridge;
using LLDMaster.Patterns.Structural.Composite;
using LLDMaster.Patterns.Structural.Decorator;
using LLDMaster.Patterns.Structural.Facade;
using LLDMaster.Patterns.Behavioral.Observer;
using LLDMaster.Patterns.Behavioral.Strategy;
using LLDMaster.Patterns.Behavioral.Command;
using LLDMaster.Patterns.Behavioral.Iterator;
using LLDMaster.Patterns.Behavioral.TemplateMethod;

// ── CREATIONAL ───────────────────────────────────────────────
SingletonDemo.Run();
FactoryMethodDemo.Run();
AbstractFactoryDemo.Run();
BuilderDemo.Run();
PrototypeDemo.Run();

// ── STRUCTURAL ───────────────────────────────────────────────
AdapterDemo.Run();
BridgeDemo.Run();
CompositeDemo.Run();
await DecoratorDemo.Run();
FacadeDemo.Run();

// ── BEHAVIORAL ───────────────────────────────────────────────
await ObserverDemo.Run();
StrategyDemo.Run();
await CommandDemo.Run();
IteratorDemo.Run();
TemplateMethodDemo.Run();
