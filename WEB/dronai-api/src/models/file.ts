import {Entity, Column, PrimaryGeneratedColumn} from "typeorm"

@Entity()
export default class File{

    @PrimaryGeneratedColumn()
    id!: number 

    @Column()
    name!: string

    @Column({
        type: "longblob"
    })
    data!: Buffer

    @Column()
    mimeType!:string
}