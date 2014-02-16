﻿// Learn more about F# at http://fsharp.net
// See the 'F# Tutorial' project for more help.

open RabbitMQ.Client
open System.Text
open MyConsumer
open MyMailboxProcessor
open System.Reactive.Linq
open MessageContracts
open StreamListener

let CreateConnectionFactory () = new ConnectionFactory()
let GetConnection (factory:ConnectionFactory) = factory.CreateConnection ()
let GetChannel (connection:IConnection) = connection.CreateModel()

let Consume (channel:IModel) queue = 
    let consumer = new MyConsumer(channel)
    channel.BasicConsume(queue, true, consumer) |> ignore
    consumer

let typeAListener = { Query = Some(Observable.filter( fun m -> match m with | TypeA(m,a) when a > 40.0f -> true | _ -> false));
                      Action = typeAMailbox }

let typeBListener = { Query = None;
                      Action = typeBMailbox }

let typeFunListener = { Query = None;
                        Action = typeFunctionMailbox }

[<EntryPoint>]
let main argv = 
    let connectionFactory = CreateConnectionFactory ()
    let connection = GetConnection connectionFactory
    let channel = GetChannel connection
    channel.QueueDeclare( "fsharp-queue", false, false, false, null) |> ignore

    let consumer = Consume channel "fsharp-queue"
    let aStream, xStream = consumer.Subject |> Observable.partition ( fun m -> match m with | TypeA(_) -> true | _ -> false)
    let bStream, cStream = consumer.Subject |> Observable.partition ( fun m -> match m with | TypeB(_) -> true | _ -> false)
    
    let attachListener stream listener =
        match listener.Query with
        | Some(query) -> stream |> query 
        | None -> stream
        |> Observable.subscribe ( listener.Action.Post)

    attachListener aStream typeAListener      
    attachListener bStream typeBListener

    while true do ()

    channel.Close()
    connection.Close()

    0 // return an integer exit code
